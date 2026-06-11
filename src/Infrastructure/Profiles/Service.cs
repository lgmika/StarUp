using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Profiles.Dtos;
using StartupConnect.Application.Profiles.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Profiles;

public sealed class ProfileService(AppDbContext dbContext) : IProfileService
{
    public async Task<ProfileDto> GetMyProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        return await BuildProfileDtoAsync(userId, includePrivateContact: true, cancellationToken);
    }

    public async Task<ProfileDto> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await BuildProfileDtoAsync(userId, includePrivateContact: false, cancellationToken);
    }

    public async Task<ProfileDto> CreateProfileAsync(ClaimsPrincipal principal, UpsertProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateProfile(request);

        var exists = await dbContext.UserProfiles.AnyAsync(profile => profile.UserId == userId, cancellationToken);
        if (exists)
        {
            throw new ApiException("Profile already exists", HttpStatusCode.Conflict);
        }

        dbContext.UserProfiles.Add(new UserProfile
        {
            UserId = userId,
            Headline = request.Headline.Trim(),
            Bio = request.Bio.Trim(),
            Location = TrimOrNull(request.Location),
            PhoneNumber = TrimOrNull(request.PhoneNumber),
            LinkedInUrl = TrimOrNull(request.LinkedInUrl),
            GitHubUrl = TrimOrNull(request.GitHubUrl),
            WebsiteUrl = TrimOrNull(request.WebsiteUrl),
            ContactVisibility = request.ContactVisibility
        });

        AddAudit(userId, "Profile.Create", "UserProfile", userId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildProfileDtoAsync(userId, includePrivateContact: true, cancellationToken);
    }

    public async Task<ProfileDto> UpdateProfileAsync(ClaimsPrincipal principal, UpsertProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateProfile(request);

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new ApiException("Profile not found", HttpStatusCode.NotFound);

        profile.Headline = request.Headline.Trim();
        profile.Bio = request.Bio.Trim();
        profile.Location = TrimOrNull(request.Location);
        profile.PhoneNumber = TrimOrNull(request.PhoneNumber);
        profile.LinkedInUrl = TrimOrNull(request.LinkedInUrl);
        profile.GitHubUrl = TrimOrNull(request.GitHubUrl);
        profile.WebsiteUrl = TrimOrNull(request.WebsiteUrl);
        profile.ContactVisibility = request.ContactVisibility;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit(userId, "Profile.Update", "UserProfile", profile.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildProfileDtoAsync(userId, includePrivateContact: true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Skills
            .OrderBy(skill => skill.Name)
            .Select(skill => new SkillDto(skill.Id, skill.Name))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SkillDto> AddUserSkillAsync(ClaimsPrincipal principal, AddUserSkillRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        var skill = await dbContext.Skills.FirstOrDefaultAsync(item => item.Id == request.SkillId, cancellationToken)
            ?? throw new ApiException("Skill not found", HttpStatusCode.NotFound);

        var exists = await dbContext.UserSkills.AnyAsync(
            userSkill => userSkill.UserId == userId && userSkill.SkillId == request.SkillId,
            cancellationToken);

        if (exists)
        {
            throw new ApiException("Skill already added", HttpStatusCode.Conflict);
        }

        if (request.YearsOfExperience is < 0 or > 60)
        {
            throw new ValidationException([new ErrorDetail("InvalidExperience", "Years of experience must be between 0 and 60", "yearsOfExperience")]);
        }

        dbContext.UserSkills.Add(new UserSkill
        {
            UserId = userId,
            SkillId = request.SkillId,
            YearsOfExperience = request.YearsOfExperience
        });

        AddAudit(userId, "Profile.Skill.Add", "Skill", request.SkillId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SkillDto(skill.Id, skill.Name, request.YearsOfExperience);
    }

    public async Task RemoveUserSkillAsync(ClaimsPrincipal principal, Guid skillId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var userSkill = await dbContext.UserSkills.FirstOrDefaultAsync(
            item => item.UserId == userId && item.SkillId == skillId,
            cancellationToken);

        if (userSkill is null)
        {
            return;
        }

        dbContext.UserSkills.Remove(userSkill);
        AddAudit(userId, "Profile.Skill.Remove", "Skill", skillId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvDto>> GetMyCvsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);

        return await dbContext.CVs
            .Include(cv => cv.File)
            .Where(cv => cv.UserId == userId && !cv.IsDeleted)
            .OrderByDescending(cv => cv.IsDefault)
            .ThenByDescending(cv => cv.CreatedAt)
            .Select(cv => MapCv(cv))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CvDto> CreateCvAsync(ClaimsPrincipal principal, CreateCvRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateCv(request.Title);

        if (request.IsDefault)
        {
            await ClearDefaultCvAsync(userId, cancellationToken);
        }

        var cv = new Cv
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Summary = TrimOrNull(request.Summary),
            ExperienceJson = TrimOrNull(request.ExperienceJson),
            EducationJson = TrimOrNull(request.EducationJson),
            Type = CvType.Internal,
            IsDefault = request.IsDefault
        };

        dbContext.CVs.Add(cv);
        AddAudit(userId, "CV.Create", "CV", cv.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCv(cv);
    }

    public async Task<CvDto> CreateUploadedCvAsync(
        ClaimsPrincipal principal,
        string originalFileName,
        string storedFileName,
        string storagePath,
        string contentType,
        long sizeInBytes,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var file = new StoredFile
        {
            OwnerUserId = userId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoragePath = storagePath,
            ContentType = contentType,
            SizeInBytes = sizeInBytes
        };

        var cv = new Cv
        {
            UserId = userId,
            Title = Path.GetFileNameWithoutExtension(originalFileName),
            Type = CvType.UploadedPdf,
            File = file
        };

        dbContext.Files.Add(file);
        dbContext.CVs.Add(cv);
        AddAudit(userId, "CV.Upload", "CV", cv.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCv(cv);
    }

    public async Task<CvDto> UpdateCvAsync(ClaimsPrincipal principal, Guid cvId, UpdateCvRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateCv(request.Title);

        var cv = await dbContext.CVs.Include(item => item.File)
            .FirstOrDefaultAsync(item => item.Id == cvId && item.UserId == userId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("CV not found", HttpStatusCode.NotFound);

        if (request.IsDefault)
        {
            await ClearDefaultCvAsync(userId, cancellationToken);
        }

        cv.Title = request.Title.Trim();
        cv.Summary = TrimOrNull(request.Summary);
        cv.ExperienceJson = TrimOrNull(request.ExperienceJson);
        cv.EducationJson = TrimOrNull(request.EducationJson);
        cv.IsDefault = request.IsDefault;
        cv.UpdatedAt = DateTimeOffset.UtcNow;

        AddAudit(userId, "CV.Update", "CV", cv.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCv(cv);
    }

    public async Task DeleteCvAsync(ClaimsPrincipal principal, Guid cvId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var cv = await dbContext.CVs.FirstOrDefaultAsync(item => item.Id == cvId && item.UserId == userId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("CV not found", HttpStatusCode.NotFound);

        cv.IsDeleted = true;
        cv.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "CV.Delete", "CV", cv.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PortfolioDto> CreatePortfolioAsync(ClaimsPrincipal principal, CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        ValidateRequired(request.Title, "title", "Portfolio title is required");
        ValidateRequired(request.Url, "url", "Portfolio URL is required");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            throw new ValidationException([new ErrorDetail("InvalidUrl", "Portfolio URL must be an absolute HTTP/HTTPS URL", "url")]);
        }

        var portfolio = new Portfolio
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Url = request.Url.Trim(),
            Description = TrimOrNull(request.Description)
        };

        dbContext.Portfolios.Add(portfolio);
        AddAudit(userId, "Portfolio.Create", "Portfolio", portfolio.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapPortfolio(portfolio);
    }

    private async Task<ProfileDto> BuildProfileDtoAsync(Guid userId, bool includePrivateContact, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new ApiException("User not found", HttpStatusCode.NotFound);

        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var skills = await dbContext.UserSkills
            .Include(userSkill => userSkill.Skill)
            .Where(userSkill => userSkill.UserId == userId)
            .OrderBy(userSkill => userSkill.Skill.Name)
            .Select(userSkill => new SkillDto(userSkill.SkillId, userSkill.Skill.Name, userSkill.YearsOfExperience))
            .ToArrayAsync(cancellationToken);

        var portfolios = await dbContext.Portfolios
            .Where(portfolio => portfolio.UserId == userId && !portfolio.IsDeleted)
            .OrderByDescending(portfolio => portfolio.CreatedAt)
            .Select(portfolio => MapPortfolio(portfolio))
            .ToArrayAsync(cancellationToken);

        var canShowContact = includePrivateContact || profile?.ContactVisibility == ContactVisibility.Public;

        return new ProfileDto(
            user.Id,
            canShowContact ? user.Email : string.Empty,
            user.FullName,
            profile?.Headline ?? string.Empty,
            profile?.Bio ?? string.Empty,
            profile?.Location,
            canShowContact ? profile?.PhoneNumber : null,
            profile?.LinkedInUrl,
            profile?.GitHubUrl,
            profile?.WebsiteUrl,
            profile?.ContactVisibility ?? ContactVisibility.Private,
            skills,
            portfolios);
    }

    private async Task ClearDefaultCvAsync(Guid userId, CancellationToken cancellationToken)
    {
        var defaultCvs = await dbContext.CVs
            .Where(cv => cv.UserId == userId && cv.IsDefault && !cv.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var cv in defaultCvs)
        {
            cv.IsDefault = false;
        }
    }

    private static CvDto MapCv(Cv cv)
    {
        return new CvDto(
            cv.Id,
            cv.Title,
            cv.Summary,
            cv.ExperienceJson,
            cv.EducationJson,
            cv.Type.ToString(),
            cv.FileId,
            cv.File?.OriginalFileName,
            cv.IsDefault,
            cv.CreatedAt);
    }

    private static PortfolioDto MapPortfolio(Portfolio portfolio)
    {
        return new PortfolioDto(portfolio.Id, portfolio.Title, portfolio.Url, portfolio.Description, portfolio.CreatedAt);
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId
        });
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("nameid")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }

    private static void ValidateProfile(UpsertProfileRequest request)
    {
        ValidateRequired(request.Headline, "headline", "Headline is required");
        ValidateRequired(request.Bio, "bio", "Bio is required");

        if (request.Headline.Trim().Length > 160)
        {
            throw new ValidationException([new ErrorDetail("HeadlineTooLong", "Headline must be at most 160 characters", "headline")]);
        }

        if (request.Bio.Trim().Length > 2000)
        {
            throw new ValidationException([new ErrorDetail("BioTooLong", "Bio must be at most 2000 characters", "bio")]);
        }
    }

    private static void ValidateCv(string title)
    {
        ValidateRequired(title, "title", "CV title is required");

        if (title.Trim().Length > 160)
        {
            throw new ValidationException([new ErrorDetail("TitleTooLong", "CV title must be at most 160 characters", "title")]);
        }
    }

    private static void ValidateRequired(string? value, string field, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException([new ErrorDetail("Required", message, field)]);
        }
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}


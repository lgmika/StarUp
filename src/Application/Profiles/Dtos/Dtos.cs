using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Profiles.Dtos;

public sealed record ProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string Headline,
    string Bio,
    string? Location,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    ContactVisibility ContactVisibility,
    IReadOnlyCollection<SkillDto> Skills,
    IReadOnlyCollection<PortfolioDto> Portfolios);

public sealed record UpsertProfileRequest(
    string Headline,
    string Bio,
    string? Location,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? WebsiteUrl,
    ContactVisibility ContactVisibility);

public sealed record SkillDto(Guid Id, string Name, int? YearsOfExperience = null);

public sealed record AddUserSkillRequest(Guid SkillId, int? YearsOfExperience);

public sealed record CvDto(
    Guid Id,
    string Title,
    string? Summary,
    string? ExperienceJson,
    string? EducationJson,
    string Type,
    Guid? FileId,
    string? FileName,
    bool IsDefault,
    DateTimeOffset CreatedAt);

public sealed record CreateCvRequest(
    string Title,
    string? Summary,
    string? ExperienceJson,
    string? EducationJson,
    bool IsDefault);

public sealed record UpdateCvRequest(
    string Title,
    string? Summary,
    string? ExperienceJson,
    string? EducationJson,
    bool IsDefault);

public sealed record PortfolioDto(
    Guid Id,
    string Title,
    string Url,
    string? Description,
    DateTimeOffset CreatedAt);

public sealed record CreatePortfolioRequest(
    string Title,
    string Url,
    string? Description);


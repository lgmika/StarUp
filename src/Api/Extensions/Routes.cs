using StartupConnect.Application.AI.Dtos;
using StartupConnect.Application.AI.Interfaces;
using StartupConnect.Application.Applications.Dtos;
using StartupConnect.Application.Applications.Interfaces;
using StartupConnect.Application.Auth.Dtos;
using StartupConnect.Application.Auth.Interfaces;
using StartupConnect.Application.Investors.Dtos;
using StartupConnect.Application.Investors.Interfaces;
using StartupConnect.Application.Moderation.Dtos;
using StartupConnect.Application.Moderation.Interfaces;
using StartupConnect.Application.Nda.Dtos;
using StartupConnect.Application.Nda.Interfaces;
using StartupConnect.Application.Profiles.Dtos;
using StartupConnect.Application.Profiles.Interfaces;
using StartupConnect.Application.Projects.Dtos;
using StartupConnect.Application.Projects.Interfaces;
using StartupConnect.Api.Authorization;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapStartupConnectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");

        api.MapGet("/", () => Results.Ok(ApiResponse<object>.Ok(new
        {
            name = "StartupConnect API",
            version = "v1"
        })))
        .WithName("ApiInfo");

        var auth = api.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", async (
            RegisterRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.RegisterAsync(request, GetIpAddress(httpContext), cancellationToken);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(result, "User registered successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.LoginAsync(request, GetIpAddress(httpContext), cancellationToken);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(result, "Login completed successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/refresh-token", async (
            RefreshTokenRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.RefreshTokenAsync(request, GetIpAddress(httpContext), cancellationToken);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/logout", async (
            LogoutRequest request,
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await authService.LogoutAsync(request, GetIpAddress(httpContext), cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Logout completed successfully"));
        })
        .RequireAuthorization();

        auth.MapPost("/verify-email", async (
            VerifyEmailRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.VerifyEmailAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Email verified successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.ForgotPasswordAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<ForgotPasswordResponse>.Ok(result, "Password reset token generated"));
        })
        .AllowAnonymous();

        auth.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.ResetPasswordAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Password reset successfully"));
        })
        .AllowAnonymous();

        auth.MapGet("/me", async (
            IAuthService authService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.GetCurrentUserAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<AuthUserDto>.Ok(result));
        })
        .RequireAuthorization();

        var profiles = api.MapGroup("/profiles").WithTags("Profiles");

        profiles.MapGet("/me", async (
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.GetMyProfileAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<ProfileDto>.Ok(result));
        })
        .RequireAuthorization();

        profiles.MapPost("/me", async (
            UpsertProfileRequest request,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.CreateProfileAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<ProfileDto>.Ok(result, "Profile created successfully"));
        })
        .RequireAuthorization();

        profiles.MapPut("/me", async (
            UpsertProfileRequest request,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.UpdateProfileAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<ProfileDto>.Ok(result, "Profile updated successfully"));
        })
        .RequireAuthorization();

        profiles.MapGet("/{userId:guid}", async (
            Guid userId,
            IProfileService profileService,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.GetPublicProfileAsync(userId, cancellationToken);
            return Results.Ok(ApiResponse<ProfileDto>.Ok(result));
        })
        .AllowAnonymous();

        api.MapGet("/skills", async (
            IProfileService profileService,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.GetSkillsAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<SkillDto>>.Ok(result));
        })
        .WithTags("Skills")
        .AllowAnonymous();

        var userSkills = api.MapGroup("/users/me/skills").WithTags("User Skills").RequireAuthorization();

        userSkills.MapPost("/", async (
            AddUserSkillRequest request,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.AddUserSkillAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<SkillDto>.Ok(result, "Skill added successfully"));
        });

        userSkills.MapDelete("/{skillId:guid}", async (
            Guid skillId,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await profileService.RemoveUserSkillAsync(httpContext.User, skillId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Skill removed successfully"));
        });

        var cvs = api.MapGroup("/cvs").WithTags("CVs").RequireAuthorization();

        cvs.MapGet("/me", async (
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.GetMyCvsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<CvDto>>.Ok(result));
        });

        cvs.MapPost("/", async (
            CreateCvRequest request,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.CreateCvAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<CvDto>.Ok(result, "CV created successfully"));
        });

        cvs.MapPost("/upload", async (
            IFormFile file,
            IProfileService profileService,
            HttpContext httpContext,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            ValidatePdfUpload(file);

            var uploadRoot = Path.Combine(environment.ContentRootPath, "storage", "uploads", "cvs");
            Directory.CreateDirectory(uploadRoot);

            var storedFileName = $"{Guid.NewGuid():N}.pdf";
            var storagePath = Path.Combine(uploadRoot, storedFileName);

            await using (var stream = File.Create(storagePath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var result = await profileService.CreateUploadedCvAsync(
                httpContext.User,
                file.FileName,
                storedFileName,
                storagePath,
                file.ContentType,
                file.Length,
                cancellationToken);

            return Results.Ok(ApiResponse<CvDto>.Ok(result, "CV uploaded successfully"));
        })
        .DisableAntiforgery();

        cvs.MapPut("/{cvId:guid}", async (
            Guid cvId,
            UpdateCvRequest request,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.UpdateCvAsync(httpContext.User, cvId, request, cancellationToken);
            return Results.Ok(ApiResponse<CvDto>.Ok(result, "CV updated successfully"));
        });

        cvs.MapDelete("/{cvId:guid}", async (
            Guid cvId,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await profileService.DeleteCvAsync(httpContext.User, cvId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "CV deleted successfully"));
        });

        api.MapPost("/portfolios", async (
            CreatePortfolioRequest request,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await profileService.CreatePortfolioAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<PortfolioDto>.Ok(result, "Portfolio created successfully"));
        })
        .WithTags("Portfolios")
        .RequireAuthorization();

        var projects = api.MapGroup("/projects").WithTags("Projects");

        projects.MapGet("/", async (
            string? search,
            IProjectService projectService,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.ListProjectsAsync(search, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectSummaryDto>>.Ok(result));
        })
        .AllowAnonymous();

        projects.MapGet("/{projectId:guid}", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.GetProjectAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectDetailDto>.Ok(result));
        })
        .AllowAnonymous();

        projects.MapPost("/drafts", async (
            CreateProjectDraftRequest request,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.CreateDraftAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<ProjectDetailDto>.Ok(result, "Project draft created successfully"));
        })
        .RequireAuthorization();

        projects.MapPut("/{projectId:guid}", async (
            Guid projectId,
            UpdateProjectRequest request,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.UpdateProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<ProjectDetailDto>.Ok(result, "Project updated successfully"));
        })
        .RequireAuthorization();

        projects.MapDelete("/{projectId:guid}", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await projectService.DeleteProjectAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project deleted successfully"));
        })
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/submit-review", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await projectService.SubmitReviewAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project submitted for review"));
        })
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/close", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await projectService.CloseProjectAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project closed successfully"));
        })
        .RequireAuthorization();

        projects.MapGet("/me/owned", async (
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.GetOwnedProjectsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectSummaryDto>>.Ok(result));
        })
        .RequireAuthorization();

        projects.MapGet("/me/joined", async (
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.GetJoinedProjectsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectSummaryDto>>.Ok(result));
        })
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/versions", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.GetVersionsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectVersionDto>>.Ok(result));
        })
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/save", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await projectService.SaveProjectAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project saved successfully"));
        })
        .RequireAuthorization();

        projects.MapDelete("/{projectId:guid}/save", async (
            Guid projectId,
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await projectService.UnsaveProjectAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project unsaved successfully"));
        })
        .RequireAuthorization();

        api.MapGet("/users/me/saved-projects", async (
            IProjectService projectService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await projectService.GetSavedProjectsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectSummaryDto>>.Ok(result));
        })
        .WithTags("Projects")
        .RequireAuthorization();

        var aiProjects = projects.MapGroup("/{projectId:guid}/ai").WithTags("AI").RequireAuthorization();

        aiProjects.MapPost("/suggestions", async (
            Guid projectId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.CreateProjectSuggestionsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<AIRecommendationDto>>.Ok(result, "AI suggestions generated"));
        });

        aiProjects.MapPost("/review", async (
            Guid projectId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.CreateProjectReviewAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<AIReviewDto>.Ok(result, "AI review generated"));
        });

        aiProjects.MapGet("/reviews", async (
            Guid projectId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.GetProjectReviewsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<AIReviewDto>>.Ok(result));
        });

        aiProjects.MapGet("/reviews/latest", async (
            Guid projectId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.GetLatestProjectReviewAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<AIReviewDto>.Ok(result));
        });

        aiProjects.MapPost("/investor-summary", async (
            Guid projectId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.CreateInvestorSummaryAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<AITextResponse>.Ok(result, "Investor summary generated"));
        });

        api.MapPost("/ai/recommendations/{recommendationId:guid}/apply", async (
            Guid recommendationId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.ApplyRecommendationAsync(httpContext.User, recommendationId, cancellationToken);
            return Results.Ok(ApiResponse<ApplyAIRecommendationResponse>.Ok(result, "AI recommendation marked as applied"));
        })
        .WithTags("AI")
        .RequireAuthorization();

        api.MapPost("/applications/{applicationId:guid}/ai/cover-letter", async (
            Guid applicationId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.CreateCoverLetterAsync(httpContext.User, applicationId, cancellationToken);
            return Results.Ok(ApiResponse<AITextResponse>.Ok(result, "Cover letter generated"));
        })
        .WithTags("AI")
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/investor-summary", async (
            Guid projectId,
            IAIService aiService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await aiService.CreateInvestorSummaryAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<AITextResponse>.Ok(result, "Investor summary generated"));
        })
        .RequireAuthorization();

        var moderator = api.MapGroup("/moderator")
            .WithTags("Moderator")
            .RequireAuthorization(AuthorizationPolicies.ModeratorOrAdmin);

        moderator.MapGet("/dashboard", async (
            IModeratorService moderatorService,
            CancellationToken cancellationToken) =>
        {
            var result = await moderatorService.GetDashboardAsync(cancellationToken);
            return Results.Ok(ApiResponse<ModeratorDashboardDto>.Ok(result));
        });

        moderator.MapGet("/projects/pending", async (
            IModeratorService moderatorService,
            CancellationToken cancellationToken) =>
        {
            var result = await moderatorService.GetPendingProjectsAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ModeratorProjectQueueItemDto>>.Ok(result));
        });

        moderator.MapGet("/projects/{projectId:guid}", async (
            Guid projectId,
            IModeratorService moderatorService,
            CancellationToken cancellationToken) =>
        {
            var result = await moderatorService.GetProjectAsync(projectId, cancellationToken);
            return Results.Ok(ApiResponse<ModeratorProjectDetailDto>.Ok(result));
        });

        moderator.MapPost("/projects/{projectId:guid}/approve", async (
            Guid projectId,
            ModerationDecisionRequest request,
            IModeratorService moderatorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await moderatorService.ApproveProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project approved successfully"));
        });

        moderator.MapPost("/projects/{projectId:guid}/request-improvement", async (
            Guid projectId,
            ModerationDecisionRequest request,
            IModeratorService moderatorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await moderatorService.RequestImprovementAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Improvement requested successfully"));
        });

        moderator.MapPost("/projects/{projectId:guid}/reject", async (
            Guid projectId,
            ModerationDecisionRequest request,
            IModeratorService moderatorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await moderatorService.RejectProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project rejected successfully"));
        });

        moderator.MapPost("/projects/{projectId:guid}/hide", async (
            Guid projectId,
            ModerationDecisionRequest request,
            IModeratorService moderatorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await moderatorService.HideProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project hidden successfully"));
        });

        moderator.MapPost("/projects/{projectId:guid}/restore", async (
            Guid projectId,
            ModerationDecisionRequest request,
            IModeratorService moderatorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await moderatorService.RestoreProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project restored successfully"));
        });

        var applications = projects.MapGroup("/{projectId:guid}/applications")
            .WithTags("Applications")
            .RequireAuthorization();

        applications.MapPost("/", async (
            Guid projectId,
            ApplyProjectRequest request,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.ApplyAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<ApplicationDto>.Ok(result, "Application submitted successfully"));
        });

        applications.MapGet("/", async (
            Guid projectId,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.GetProjectApplicationsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ApplicationDto>>.Ok(result));
        });

        applications.MapGet("/{applicationId:guid}", async (
            Guid projectId,
            Guid applicationId,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.GetProjectApplicationAsync(httpContext.User, projectId, applicationId, cancellationToken);
            return Results.Ok(ApiResponse<ApplicationDetailDto>.Ok(result));
        });

        applications.MapPost("/{applicationId:guid}/withdraw", async (
            Guid projectId,
            Guid applicationId,
            ApplicationDecisionRequest request,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await applicationService.WithdrawAsync(httpContext.User, projectId, applicationId, request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Application withdrawn successfully"));
        });

        applications.MapPost("/{applicationId:guid}/shortlist", async (
            Guid projectId,
            Guid applicationId,
            ApplicationDecisionRequest request,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.ShortlistAsync(httpContext.User, projectId, applicationId, request, cancellationToken);
            return Results.Ok(ApiResponse<ApplicationDto>.Ok(result, "Application shortlisted successfully"));
        });

        applications.MapPost("/{applicationId:guid}/interview", async (
            Guid projectId,
            Guid applicationId,
            ApplicationDecisionRequest request,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.InterviewAsync(httpContext.User, projectId, applicationId, request, cancellationToken);
            return Results.Ok(ApiResponse<ApplicationDto>.Ok(result, "Application moved to interviewing"));
        });

        applications.MapPost("/{applicationId:guid}/accept", async (
            Guid projectId,
            Guid applicationId,
            ApplicationDecisionRequest request,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.AcceptAsync(httpContext.User, projectId, applicationId, request, cancellationToken);
            return Results.Ok(ApiResponse<ApplicationDto>.Ok(result, "Application accepted successfully"));
        });

        applications.MapPost("/{applicationId:guid}/reject", async (
            Guid projectId,
            Guid applicationId,
            ApplicationDecisionRequest request,
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.RejectAsync(httpContext.User, projectId, applicationId, request, cancellationToken);
            return Results.Ok(ApiResponse<ApplicationDto>.Ok(result, "Application rejected successfully"));
        });

        api.MapGet("/users/me/applications", async (
            IApplicationService applicationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await applicationService.GetMyApplicationsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ApplicationDto>>.Ok(result));
        })
        .WithTags("Applications")
        .RequireAuthorization();

        var investors = api.MapGroup("/investors")
            .WithTags("Investors")
            .RequireAuthorization();

        investors.MapGet("/me/profile", async (
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.GetMyProfileAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<InvestorProfileDto>.Ok(result));
        });

        investors.MapPost("/me/profile", async (
            UpsertInvestorProfileRequest request,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.CreateProfileAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<InvestorProfileDto>.Ok(result, "Investor profile created successfully"));
        });

        investors.MapPut("/me/profile", async (
            UpsertInvestorProfileRequest request,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.UpdateProfileAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<InvestorProfileDto>.Ok(result, "Investor profile updated successfully"));
        });

        investors.MapGet("/projects", async (
            string? search,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.GetInvestorProjectsAsync(httpContext.User, search, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<InvestorProjectDiscoveryDto>>.Ok(result));
        });

        investors.MapGet("/me/interests", async (
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.GetMyInterestsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<InvestorInterestDto>>.Ok(result));
        });

        projects.MapPost("/{projectId:guid}/investor-interests", async (
            Guid projectId,
            CreateInvestorInterestRequest request,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.CreateInterestAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<InvestorInterestDto>.Ok(result, "Investor interest submitted successfully"));
        })
        .WithTags("Investor Interests")
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/investor-interests", async (
            Guid projectId,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.GetProjectInterestsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<InvestorInterestDto>>.Ok(result));
        })
        .WithTags("Investor Interests")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/investor-interests/{interestId:guid}/accept", async (
            Guid projectId,
            Guid interestId,
            InvestorInterestDecisionRequest request,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.AcceptInterestAsync(httpContext.User, projectId, interestId, request, cancellationToken);
            return Results.Ok(ApiResponse<InvestorInterestDto>.Ok(result, "Investor interest accepted successfully"));
        })
        .WithTags("Investor Interests")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/investor-interests/{interestId:guid}/reject", async (
            Guid projectId,
            Guid interestId,
            InvestorInterestDecisionRequest request,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.RejectInterestAsync(httpContext.User, projectId, interestId, request, cancellationToken);
            return Results.Ok(ApiResponse<InvestorInterestDto>.Ok(result, "Investor interest rejected successfully"));
        })
        .WithTags("Investor Interests")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/investor-interests/{interestId:guid}/request-more-info", async (
            Guid projectId,
            Guid interestId,
            InvestorInterestDecisionRequest request,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.RequestMoreInfoAsync(httpContext.User, projectId, interestId, request, cancellationToken);
            return Results.Ok(ApiResponse<InvestorInterestDto>.Ok(result, "More information requested successfully"));
        })
        .WithTags("Investor Interests")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/investor-interests/{interestId:guid}/withdraw", async (
            Guid projectId,
            Guid interestId,
            IInvestorService investorService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await investorService.WithdrawInterestAsync(httpContext.User, projectId, interestId, cancellationToken);
            return Results.Ok(ApiResponse<InvestorInterestDto>.Ok(result, "Investor interest withdrawn successfully"));
        })
        .WithTags("Investor Interests")
        .RequireAuthorization();

        var nda = api.MapGroup("/nda")
            .WithTags("NDA");

        nda.MapGet("/templates", async (
            INdaService ndaService,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.GetTemplatesAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<NdaTemplateDto>>.Ok(result));
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        nda.MapPost("/templates", async (
            CreateNdaTemplateRequest request,
            INdaService ndaService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.CreateTemplateAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<NdaTemplateDto>.Ok(result, "NDA template created successfully"));
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        nda.MapPost("/templates/{templateId:guid}/versions", async (
            Guid templateId,
            CreateNdaTemplateVersionRequest request,
            INdaService ndaService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.CreateTemplateVersionAsync(httpContext.User, templateId, request, cancellationToken);
            return Results.Ok(ApiResponse<NdaTemplateVersionDto>.Ok(result, "NDA template version created successfully"));
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        projects.MapGet("/{projectId:guid}/nda/current", async (
            Guid projectId,
            INdaService ndaService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.GetCurrentProjectNdaAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<CurrentProjectNdaDto>.Ok(result));
        })
        .WithTags("NDA")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/nda/accept", async (
            Guid projectId,
            INdaService ndaService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.AcceptProjectNdaAsync(
                httpContext.User,
                projectId,
                GetIpAddress(httpContext),
                httpContext.Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return Results.Ok(ApiResponse<NdaAgreementDto>.Ok(result, "NDA accepted successfully"));
        })
        .WithTags("NDA")
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/nda/agreements", async (
            Guid projectId,
            INdaService ndaService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.GetProjectAgreementsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<NdaAgreementDto>>.Ok(result));
        })
        .WithTags("NDA")
        .RequireAuthorization();

        api.MapGet("/users/me/nda-agreements", async (
            INdaService ndaService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await ndaService.GetMyAgreementsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<NdaAgreementDto>>.Ok(result));
        })
        .WithTags("NDA")
        .RequireAuthorization();

        return endpoints;
    }

    private static string? GetIpAddress(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static void ValidatePdfUpload(IFormFile file)
    {
        const long maxSizeInBytes = 5 * 1024 * 1024;

        if (file.Length == 0)
        {
            throw new StartupConnect.Shared.Exceptions.ValidationException(
                [new StartupConnect.Shared.Responses.ErrorDetail("EmptyFile", "CV file is required", "file")]);
        }

        if (file.Length > maxSizeInBytes)
        {
            throw new StartupConnect.Shared.Exceptions.ValidationException(
                [new StartupConnect.Shared.Responses.ErrorDetail("FileTooLarge", "CV file must be at most 5 MB", "file")]);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new StartupConnect.Shared.Exceptions.ValidationException(
                [new StartupConnect.Shared.Responses.ErrorDetail("InvalidFileType", "Only PDF CV uploads are allowed", "file")]);
        }
    }
}

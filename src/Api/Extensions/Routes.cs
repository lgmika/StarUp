using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartupConnect.Api.Security;
using StartupConnect.Application.Activities.Dtos;
using StartupConnect.Application.Activities.Interfaces;
using StartupConnect.Application.Admin.Dtos;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Application.AI.Dtos;
using StartupConnect.Application.AI.Interfaces;
using StartupConnect.Application.Applications.Dtos;
using StartupConnect.Application.Applications.Interfaces;
using StartupConnect.Application.Auth.Dtos;
using StartupConnect.Application.Auth.Interfaces;
using StartupConnect.Application.BackgroundJobs.Dtos;
using StartupConnect.Application.BackgroundJobs.Interfaces;
using StartupConnect.Application.Chat.Dtos;
using StartupConnect.Application.Chat.Interfaces;
using StartupConnect.Application.Dashboards.Dtos;
using StartupConnect.Application.Dashboards.Interfaces;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Files.Interfaces;
using StartupConnect.Application.Investors.Dtos;
using StartupConnect.Application.Investors.Interfaces;
using StartupConnect.Application.Interviews.Dtos;
using StartupConnect.Application.Interviews.Interfaces;
using StartupConnect.Application.Moderation.Dtos;
using StartupConnect.Application.Moderation.Interfaces;
using StartupConnect.Application.Nda.Dtos;
using StartupConnect.Application.Nda.Interfaces;
using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Application.Notifications.Interfaces;
using StartupConnect.Application.Profiles.Dtos;
using StartupConnect.Application.Profiles.Interfaces;
using StartupConnect.Application.ProjectTeams.Dtos;
using StartupConnect.Application.ProjectTeams.Interfaces;
using StartupConnect.Application.Projects.Dtos;
using StartupConnect.Application.Projects.Interfaces;
using StartupConnect.Application.Recommendations.Dtos;
using StartupConnect.Application.Recommendations.Interfaces;
using StartupConnect.Application.Reports.Dtos;
using StartupConnect.Application.Reports.Interfaces;
using StartupConnect.Application.Search.Dtos;
using StartupConnect.Application.Search.Interfaces;
using StartupConnect.Application.Subscriptions.Dtos;
using StartupConnect.Application.Subscriptions.Interfaces;
using StartupConnect.Api.Authorization;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
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

        api.MapGet("/feed", async (
            int? page,
            int? pageSize,
            IActivityService activityService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await activityService.GetFeedAsync(httpContext.User, new ActivityQuery(page ?? 1, pageSize ?? 20), cancellationToken);
            return Results.Ok(ApiResponse<ActivityListResponse>.Ok(result));
        })
        .WithTags("Activity Feed")
        .AllowAnonymous();

        var search = api.MapGroup("/search").WithTags("Search");

        search.MapGet("/projects", async (
            string? keyword,
            ProjectStatus? status,
            ProjectStage? stage,
            string? requiredRole,
            Guid? requiredSkillId,
            string? location,
            bool? remote,
            DateTimeOffset? createdFrom,
            DateTimeOffset? createdTo,
            string? sort,
            int? page,
            int? pageSize,
            ISearchService searchService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await searchService.SearchProjectsAsync(
                httpContext.User,
                new ProjectSearchQuery(keyword, status, stage, requiredRole, requiredSkillId, location, remote, createdFrom, createdTo, sort, page ?? 1, pageSize ?? 20),
                cancellationToken);

            return Results.Ok(ApiResponse<SearchResultPage<ProjectSearchItemDto>>.Ok(result));
        })
        .AllowAnonymous();

        search.MapGet("/members", async (
            string? keyword,
            Guid? skillId,
            int? minYearsOfExperience,
            string? location,
            bool? verifiedOnly,
            int? page,
            int? pageSize,
            ISearchService searchService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await searchService.SearchMembersAsync(
                httpContext.User,
                new MemberSearchQuery(keyword, skillId, minYearsOfExperience, location, verifiedOnly ?? false, page ?? 1, pageSize ?? 20),
                cancellationToken);

            return Results.Ok(ApiResponse<SearchResultPage<MemberSearchItemDto>>.Ok(result));
        })
        .AllowAnonymous();

        search.MapGet("/investors", async (
            string? keyword,
            decimal? minTicketSize,
            decimal? maxTicketSize,
            int? page,
            int? pageSize,
            ISearchService searchService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await searchService.SearchInvestorsAsync(
                httpContext.User,
                new InvestorSearchQuery(keyword, minTicketSize, maxTicketSize, page ?? 1, pageSize ?? 20),
                cancellationToken);

            return Results.Ok(ApiResponse<SearchResultPage<InvestorSearchItemDto>>.Ok(result));
        })
        .RequireAuthorization();

        search.MapGet("/suggestions", async (
            string? keyword,
            int? limit,
            ISearchService searchService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await searchService.GetSuggestionsAsync(
                httpContext.User,
                new SearchSuggestionQuery(keyword, limit ?? 10),
                cancellationToken);

            return Results.Ok(ApiResponse<SearchSuggestionsResponse>.Ok(result));
        })
        .AllowAnonymous();

        var recommendations = api.MapGroup("/recommendations")
            .WithTags("Recommendations")
            .RequireAuthorization();

        recommendations.MapGet("/projects", async (
            int? page,
            int? pageSize,
            IRecommendationService recommendationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await recommendationService.GetProjectRecommendationsAsync(httpContext.User, page ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(ApiResponse<RecommendationListResponse<ProjectRecommendationDto>>.Ok(result));
        });

        recommendations.MapGet("/members", async (
            int? page,
            int? pageSize,
            IRecommendationService recommendationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await recommendationService.GetMemberRecommendationsAsync(httpContext.User, page ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(ApiResponse<RecommendationListResponse<MemberRecommendationDto>>.Ok(result));
        });

        recommendations.MapPost("/{recommendationId:guid}/dismiss", async (
            Guid recommendationId,
            IRecommendationService recommendationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await recommendationService.DismissAsync(httpContext.User, recommendationId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Recommendation dismissed"));
        });

        var dashboard = api.MapGroup("/dashboard")
            .WithTags("Dashboards")
            .RequireAuthorization();

        dashboard.MapGet("/me", async (
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? timezoneOffsetMinutes,
            IDashboardService dashboardService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await dashboardService.GetMyDashboardAsync(
                httpContext.User,
                new DashboardQuery(from, to, timezoneOffsetMinutes ?? 0),
                cancellationToken);

            return Results.Ok(ApiResponse<UserDashboardDto>.Ok(result));
        });

        var subscriptions = api.MapGroup("/subscriptions")
            .WithTags("Subscriptions");

        subscriptions.MapGet("/plans", async (
            ISubscriptionService subscriptionService,
            CancellationToken cancellationToken) =>
        {
            var result = await subscriptionService.GetPlansAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<SubscriptionPlanDto>>.Ok(result));
        })
        .AllowAnonymous();

        subscriptions.MapGet("/me", async (
            ISubscriptionService subscriptionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await subscriptionService.GetMySubscriptionAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<SubscriptionDto>.Ok(result));
        })
        .RequireAuthorization();

        subscriptions.MapPost("/checkout", async (
            CheckoutRequest request,
            ISubscriptionService subscriptionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await subscriptionService.CreateCheckoutAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<CheckoutResponse>.Ok(result, "Checkout session created"));
        })
        .RequireAuthorization();

        subscriptions.MapPost("/cancel", async (
            ISubscriptionService subscriptionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await subscriptionService.CancelAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<SubscriptionDto>.Ok(result, "Subscription cancelled"));
        })
        .RequireAuthorization();

        subscriptions.MapPost("/resume", async (
            ISubscriptionService subscriptionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await subscriptionService.ResumeAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<SubscriptionDto>.Ok(result, "Subscription resumed"));
        })
        .RequireAuthorization();

        api.MapPost("/webhooks/payments", async (
            ISubscriptionService subscriptionService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = httpContext.Request.Headers["Stripe-Signature"].FirstOrDefault()
                ?? httpContext.Request.Headers["X-Payment-Signature"].FirstOrDefault();
            var result = await subscriptionService.HandleWebhookAsync(payload, signature, cancellationToken);
            return Results.Ok(ApiResponse<PaymentWebhookResult>.Ok(result));
        })
        .WithTags("Payment Webhooks")
        .AllowAnonymous();

        var auth = api.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", async (
            RegisterRequest request,
            IAuthService authService,
            IOptions<StartupConnectSecurityOptions> securityOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.RegisterAsync(request, GetIpAddress(httpContext), cancellationToken);
            AuthCookieHelper.AppendRefreshTokenCookie(httpContext.Response, result, securityOptions.Value);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(result, "User registered successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            IOptions<StartupConnectSecurityOptions> securityOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.LoginAsync(request, GetIpAddress(httpContext), cancellationToken);
            AuthCookieHelper.AppendRefreshTokenCookie(httpContext.Response, result, securityOptions.Value);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(result, "Login completed successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/refresh-token", async (
            RefreshTokenRequest request,
            IAuthService authService,
            IOptions<StartupConnectSecurityOptions> securityOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.RefreshTokenAsync(
                WithRefreshTokenCookieFallback(request, httpContext, securityOptions.Value),
                GetIpAddress(httpContext),
                cancellationToken);

            AuthCookieHelper.AppendRefreshTokenCookie(httpContext.Response, result, securityOptions.Value);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed successfully"));
        })
        .AllowAnonymous();

        auth.MapPost("/logout", async (
            LogoutRequest request,
            IAuthService authService,
            IOptions<StartupConnectSecurityOptions> securityOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await authService.LogoutAsync(
                WithRefreshTokenCookieFallback(request, httpContext, securityOptions.Value),
                GetIpAddress(httpContext),
                cancellationToken);

            AuthCookieHelper.DeleteRefreshTokenCookie(httpContext.Response, securityOptions.Value);
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

        auth.MapPost("/resend-verification", async (
            ResendVerificationRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            await authService.ResendVerificationAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "If the account exists and is not verified, a verification email has been sent"));
        })
        .AllowAnonymous();

        auth.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            var result = await authService.ForgotPasswordAsync(request, cancellationToken);
            return Results.Ok(ApiResponse<ForgotPasswordResponse>.Ok(result, "If the email exists, a password reset email has been sent"));
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
            IFileService fileService,
            IFileStorageService fileStorageService,
            IProfileService profileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            var storedFile = await fileService.UploadCvAsync(
                httpContext.User,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                cancellationToken);

            try
            {
                var result = await profileService.CreateUploadedCvAsync(
                    httpContext.User,
                    storedFile,
                    cancellationToken);

                return Results.Ok(ApiResponse<CvDto>.Ok(result, "CV uploaded successfully"));
            }
            catch
            {
                await fileStorageService.DeleteAsync(storedFile.StoragePath, cancellationToken);
                throw;
            }
        })
        .DisableAntiforgery();

        api.MapGet("/files/{fileId:guid}/download-url", async (
            Guid fileId,
            IFileService fileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await fileService.CreateDownloadUrlAsync(httpContext.User, fileId, cancellationToken);
            return Results.Ok(ApiResponse<FileDownloadUrlResponse>.Ok(result));
        })
        .WithTags("Files")
        .RequireAuthorization();

        api.MapGet("/files/me", async (
            int? page,
            int? pageSize,
            IFileService fileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await fileService.GetMyFilesAsync(httpContext.User, page ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(ApiResponse<FileListResponse>.Ok(result));
        })
        .WithTags("Files")
        .RequireAuthorization();

        api.MapDelete("/files/{fileId:guid}", async (
            Guid fileId,
            IFileService fileService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await fileService.DeleteAsync(httpContext.User, fileId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "File deleted"));
        })
        .WithTags("Files")
        .RequireAuthorization();

        api.MapGet("/files/download", async (
            string path,
            long expires,
            string signature,
            IFileStorageService fileStorageService,
            AppDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (!fileStorageService.ValidateDownloadUrl(path, expires, signature))
            {
                return Results.Unauthorized();
            }

            var file = await dbContext.Files.FirstOrDefaultAsync(item => item.StoragePath == path && !item.IsDeleted, cancellationToken);
            if (file is null)
            {
                return Results.NotFound();
            }

            var stream = await fileStorageService.OpenReadAsync(file.StoragePath, cancellationToken);
            return Results.File(stream, file.ContentType, file.OriginalFileName);
        })
        .WithTags("Files")
        .AllowAnonymous();

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

        projects.MapGet("/{projectId:guid}/activities", async (
            Guid projectId,
            int? page,
            int? pageSize,
            IActivityService activityService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await activityService.GetProjectActivitiesAsync(httpContext.User, projectId, new ActivityQuery(page ?? 1, pageSize ?? 20), cancellationToken);
            return Results.Ok(ApiResponse<ActivityListResponse>.Ok(result));
        })
        .WithTags("Activity Feed")
        .AllowAnonymous();

        projects.MapGet("/{projectId:guid}/recommended-members", async (
            Guid projectId,
            int? page,
            int? pageSize,
            IRecommendationService recommendationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await recommendationService.GetRecommendedMembersForProjectAsync(httpContext.User, projectId, page ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(ApiResponse<RecommendationListResponse<MemberRecommendationDto>>.Ok(result));
        })
        .WithTags("Recommendations")
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/dashboard", async (
            Guid projectId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? timezoneOffsetMinutes,
            IDashboardService dashboardService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await dashboardService.GetFounderProjectDashboardAsync(
                httpContext.User,
                projectId,
                new DashboardQuery(from, to, timezoneOffsetMinutes ?? 0),
                cancellationToken);

            return Results.Ok(ApiResponse<FounderProjectDashboardDto>.Ok(result));
        })
        .WithTags("Dashboards")
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

        api.MapPost("/applications/{applicationId:guid}/interviews", async (
            Guid applicationId,
            CreateInterviewRequest request,
            IInterviewService interviewService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await interviewService.CreateAsync(httpContext.User, applicationId, request, cancellationToken);
            return Results.Ok(ApiResponse<InterviewDto>.Ok(result, "Interview scheduled"));
        })
        .WithTags("Interviews")
        .RequireAuthorization();

        api.MapGet("/applications/{applicationId:guid}/interviews", async (
            Guid applicationId,
            IInterviewService interviewService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await interviewService.GetApplicationInterviewsAsync(httpContext.User, applicationId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<InterviewDto>>.Ok(result));
        })
        .WithTags("Interviews")
        .RequireAuthorization();

        api.MapGet("/users/me/interviews", async (
            IInterviewService interviewService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await interviewService.GetMyInterviewsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<InterviewDto>>.Ok(result));
        })
        .WithTags("Interviews")
        .RequireAuthorization();

        api.MapPut("/interviews/{interviewId:guid}", async (
            Guid interviewId,
            UpdateInterviewRequest request,
            IInterviewService interviewService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await interviewService.UpdateAsync(httpContext.User, interviewId, request, cancellationToken);
            return Results.Ok(ApiResponse<InterviewDto>.Ok(result, "Interview updated"));
        })
        .WithTags("Interviews")
        .RequireAuthorization();

        api.MapPost("/interviews/{interviewId:guid}/cancel", async (
            Guid interviewId,
            InterviewDecisionRequest request,
            IInterviewService interviewService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await interviewService.CancelAsync(httpContext.User, interviewId, request, cancellationToken);
            return Results.Ok(ApiResponse<InterviewDto>.Ok(result, "Interview cancelled"));
        })
        .WithTags("Interviews")
        .RequireAuthorization();

        api.MapPost("/interviews/{interviewId:guid}/complete", async (
            Guid interviewId,
            InterviewDecisionRequest request,
            IInterviewService interviewService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await interviewService.CompleteAsync(httpContext.User, interviewId, request, cancellationToken);
            return Results.Ok(ApiResponse<InterviewDto>.Ok(result, "Interview completed"));
        })
        .WithTags("Interviews")
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

        var notifications = api.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        notifications.MapGet("/", async (
            string? status,
            bool? unread,
            NotificationType? type,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? page,
            int? pageSize,
            INotificationService notificationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            status = unread.HasValue ? (unread.Value ? "unread" : "all") : status;
            var query = new NotificationQuery(status, type, from, to, page ?? 1, pageSize ?? 20);
            var result = await notificationService.GetMyNotificationsAsync(httpContext.User, query, cancellationToken);
            return Results.Ok(ApiResponse<NotificationListResponse>.Ok(result));
        });

        notifications.MapGet("/unread-count", async (
            INotificationService notificationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await notificationService.GetUnreadCountAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(new { unreadCount = result }));
        });

        notifications.MapPost("/{notificationId:guid}/read", async (
            Guid notificationId,
            INotificationService notificationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await notificationService.MarkAsReadAsync(httpContext.User, notificationId, cancellationToken);
            return Results.Ok(ApiResponse<NotificationDto>.Ok(result, "Notification marked as read"));
        });

        notifications.MapPost("/read-all", async (
            INotificationService notificationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await notificationService.MarkAllAsReadAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(new { updatedCount = result }, "Notifications marked as read"));
        });

        notifications.MapDelete("/{notificationId:guid}", async (
            Guid notificationId,
            INotificationService notificationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await notificationService.DeleteAsync(httpContext.User, notificationId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Notification deleted"));
        });

        var conversations = api.MapGroup("/conversations")
            .WithTags("Chat")
            .RequireAuthorization();

        conversations.MapPost("/", async (
            CreateConversationRequest request,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await chatService.CreateConversationAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<ConversationDto>.Ok(result, "Conversation created"));
        });

        conversations.MapGet("/", async (
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await chatService.GetMyConversationsAsync(httpContext.User, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ConversationDto>>.Ok(result));
        });

        conversations.MapGet("/{conversationId:guid}", async (
            Guid conversationId,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await chatService.GetConversationAsync(httpContext.User, conversationId, cancellationToken);
            return Results.Ok(ApiResponse<ConversationDto>.Ok(result));
        });

        conversations.MapGet("/{conversationId:guid}/messages", async (
            Guid conversationId,
            string? before,
            int? pageSize,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await chatService.GetMessagesAsync(httpContext.User, conversationId, before, pageSize ?? 20, cancellationToken);
            return Results.Ok(ApiResponse<MessageListResponse>.Ok(result));
        });

        conversations.MapPost("/{conversationId:guid}/messages", async (
            Guid conversationId,
            SendMessageRequest request,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await chatService.SendMessageAsync(httpContext.User, conversationId, request, cancellationToken);
            return Results.Ok(ApiResponse<MessageDto>.Ok(result, "Message sent"));
        });

        conversations.MapPost("/{conversationId:guid}/read", async (
            Guid conversationId,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await chatService.MarkReadAsync(httpContext.User, conversationId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Conversation marked as read"));
        });

        api.MapDelete("/messages/{messageId:guid}", async (
            Guid messageId,
            IChatService chatService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await chatService.DeleteMessageAsync(httpContext.User, messageId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Message deleted"));
        })
        .WithTags("Chat")
        .RequireAuthorization();

        var reports = api.MapGroup("/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        reports.MapPost("/", async (
            CreateReportRequest request,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.CreateAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<ReportDto>.Ok(result, "Report submitted"));
        });

        api.MapGet("/users/me/reports", async (
            ReportStatus? status,
            string? targetType,
            ReportReasonCode? reasonCode,
            int? page,
            int? pageSize,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var query = new ReportQuery(status, targetType, reasonCode, page ?? 1, pageSize ?? 20);
            var result = await reportService.GetMyReportsAsync(httpContext.User, query, cancellationToken);
            return Results.Ok(ApiResponse<ReportListResponse>.Ok(result));
        })
        .WithTags("Reports")
        .RequireAuthorization();

        api.MapGet("/users/me/reports/{reportId:guid}", async (
            Guid reportId,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.GetMyReportAsync(httpContext.User, reportId, cancellationToken);
            return Results.Ok(ApiResponse<ReportDetailDto>.Ok(result));
        })
        .WithTags("Reports")
        .RequireAuthorization();

        reports.MapGet("/targets/{targetType}/{targetId:guid}", async (
            string targetType,
            Guid targetId,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.GetTargetContextAsync(httpContext.User, targetType, targetId, cancellationToken);
            return Results.Ok(ApiResponse<ReportTargetContextDto>.Ok(result));
        });

        var moderator = api.MapGroup("/moderator")
            .WithTags("Moderator")
            .RequireAuthorization(AuthorizationPolicies.ModeratorOrAdmin);

        moderator.MapGet("/reports", async (
            ReportStatus? status,
            string? targetType,
            ReportReasonCode? reasonCode,
            int? page,
            int? pageSize,
            IReportService reportService,
            CancellationToken cancellationToken) =>
        {
            var query = new ReportQuery(status, targetType, reasonCode, page ?? 1, pageSize ?? 20);
            var result = await reportService.GetModeratorReportsAsync(query, cancellationToken);
            return Results.Ok(ApiResponse<ReportListResponse>.Ok(result));
        });

        moderator.MapGet("/reports/{reportId:guid}", async (
            Guid reportId,
            IReportService reportService,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.GetModeratorReportAsync(reportId, cancellationToken);
            return Results.Ok(ApiResponse<ReportDetailDto>.Ok(result));
        });

        moderator.MapPost("/reports/{reportId:guid}/assign", async (
            Guid reportId,
            ModeratorReportActionRequest request,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.AssignAsync(httpContext.User, reportId, request, cancellationToken);
            return Results.Ok(ApiResponse<ReportDetailDto>.Ok(result, "Report assigned"));
        });

        moderator.MapPost("/reports/{reportId:guid}/investigate", async (
            Guid reportId,
            ModeratorReportActionRequest request,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.InvestigateAsync(httpContext.User, reportId, request, cancellationToken);
            return Results.Ok(ApiResponse<ReportDetailDto>.Ok(result, "Report marked as investigating"));
        });

        moderator.MapPost("/reports/{reportId:guid}/resolve", async (
            Guid reportId,
            ResolveReportRequest request,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.ResolveAsync(httpContext.User, reportId, request, cancellationToken);
            return Results.Ok(ApiResponse<ReportDetailDto>.Ok(result, "Report resolved"));
        });

        moderator.MapPost("/reports/{reportId:guid}/dismiss", async (
            Guid reportId,
            ModeratorReportActionRequest request,
            IReportService reportService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await reportService.DismissAsync(httpContext.User, reportId, request, cancellationToken);
            return Results.Ok(ApiResponse<ReportDetailDto>.Ok(result, "Report dismissed"));
        });

        var admin = api.MapGroup("/admin")
            .WithTags("Admin")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        admin.MapGet("/dashboard", async (
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.GetDashboardAsync(cancellationToken);
            return Results.Ok(ApiResponse<AdminDashboardDto>.Ok(result));
        });

        admin.MapGet("/users", async (
            string? search,
            UserStatus? status,
            string? roleCode,
            int? page,
            int? pageSize,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var query = new AdminUserQuery(search, status, roleCode, page ?? 1, pageSize ?? 20);
            var result = await adminService.GetUsersAsync(query, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserListResponse>.Ok(result));
        });

        admin.MapGet("/users/{userId:guid}", async (
            Guid userId,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.GetUserAsync(userId, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result));
        });

        admin.MapPost("/users/{userId:guid}/suspend", async (
            Guid userId,
            AdminUserStatusRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.SuspendUserAsync(httpContext.User, userId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result, "User suspended"));
        });

        admin.MapPost("/users/{userId:guid}/unsuspend", async (
            Guid userId,
            AdminUserStatusRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.UnsuspendUserAsync(httpContext.User, userId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result, "User unsuspended"));
        });

        admin.MapPost("/users/{userId:guid}/ban", async (
            Guid userId,
            AdminUserStatusRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.BanUserAsync(httpContext.User, userId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result, "User banned"));
        });

        admin.MapPost("/users/{userId:guid}/unban", async (
            Guid userId,
            AdminUserStatusRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.UnbanUserAsync(httpContext.User, userId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result, "User unbanned"));
        });

        admin.MapPost("/users/{userId:guid}/roles", async (
            Guid userId,
            AdminRoleRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.AddRoleAsync(httpContext.User, userId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result, "Role added"));
        });

        admin.MapDelete("/users/{userId:guid}/roles/{roleCode}", async (
            Guid userId,
            string roleCode,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.RemoveRoleAsync(httpContext.User, userId, roleCode, cancellationToken);
            return Results.Ok(ApiResponse<AdminUserDto>.Ok(result, "Role removed"));
        });

        admin.MapGet("/roles", async (
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.GetRolesAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<AdminRoleDto>>.Ok(result));
        });

        admin.MapGet("/audit-logs", async (
            int? page,
            int? pageSize,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.GetAuditLogsAsync(page ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(ApiResponse<AdminAuditLogListResponse>.Ok(result));
        });

        admin.MapGet("/settings", async (
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.GetSettingsAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<AdminSettingDto>>.Ok(result));
        });

        admin.MapPut("/settings/{key}", async (
            string key,
            UpdateAdminSettingRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.UpdateSettingAsync(httpContext.User, key, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminSettingDto>.Ok(result, "Setting updated"));
        });

        admin.MapGet("/projects", async (
            string? search,
            ProjectStatus? status,
            ProjectStage? stage,
            ProjectVisibility? visibility,
            string? ownerEmail,
            int? page,
            int? pageSize,
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var query = new AdminProjectQuery(search, status, stage, visibility, ownerEmail, page ?? 1, pageSize ?? 20);
            var result = await adminService.GetProjectsAsync(query, cancellationToken);
            return Results.Ok(ApiResponse<AdminProjectListResponse>.Ok(result));
        });

        admin.MapPost("/projects/{projectId:guid}/hide", async (
            Guid projectId,
            AdminProjectActionRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.HideProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminProjectDto>.Ok(result, "Project hidden"));
        });

        admin.MapPost("/projects/{projectId:guid}/restore", async (
            Guid projectId,
            AdminProjectActionRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.RestoreProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminProjectDto>.Ok(result, "Project restored"));
        });

        admin.MapPost("/projects/{projectId:guid}/archive", async (
            Guid projectId,
            AdminProjectActionRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.ArchiveProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminProjectDto>.Ok(result, "Project archived"));
        });

        admin.MapPost("/projects/{projectId:guid}/close", async (
            Guid projectId,
            AdminProjectActionRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.CloseProjectAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminProjectDto>.Ok(result, "Project closed"));
        });

        admin.MapPost("/projects/{projectId:guid}/status", async (
            Guid projectId,
            AdminProjectStatusRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.ForceProjectStatusAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminProjectDto>.Ok(result, "Project status updated"));
        });

        admin.MapGet("/subscription-plans", async (
            IAdminService adminService,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.GetSubscriptionPlansAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<AdminSubscriptionPlanDto>>.Ok(result));
        });

        admin.MapPost("/subscription-plans", async (
            AdminSubscriptionPlanRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.CreateSubscriptionPlanAsync(httpContext.User, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminSubscriptionPlanDto>.Ok(result, "Subscription plan created"));
        });

        admin.MapPut("/subscription-plans/{planId:guid}", async (
            Guid planId,
            AdminSubscriptionPlanRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.UpdateSubscriptionPlanAsync(httpContext.User, planId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminSubscriptionPlanDto>.Ok(result, "Subscription plan updated"));
        });

        admin.MapPost("/subscription-plans/{planId:guid}/quotas", async (
            Guid planId,
            AdminUsageQuotaRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.CreateUsageQuotaAsync(httpContext.User, planId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUsageQuotaDto>.Ok(result, "Usage quota created"));
        });

        admin.MapPut("/subscription-plans/{planId:guid}/quotas/{quotaId:guid}", async (
            Guid planId,
            Guid quotaId,
            AdminUsageQuotaRequest request,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await adminService.UpdateUsageQuotaAsync(httpContext.User, planId, quotaId, request, cancellationToken);
            return Results.Ok(ApiResponse<AdminUsageQuotaDto>.Ok(result, "Usage quota updated"));
        });

        admin.MapDelete("/subscription-plans/{planId:guid}/quotas/{quotaId:guid}", async (
            Guid planId,
            Guid quotaId,
            string? reason,
            IAdminService adminService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await adminService.DeleteUsageQuotaAsync(httpContext.User, planId, quotaId, reason, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Usage quota deleted"));
        });

        admin.MapGet("/background-jobs", async (
            int? limit,
            IBackgroundJobService backgroundJobService,
            CancellationToken cancellationToken) =>
        {
            var result = await backgroundJobService.GetRecentExecutionsAsync(limit ?? 50, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<BackgroundJobExecutionDto>>.Ok(result));
        });

        admin.MapPost("/background-jobs/run", async (
            IBackgroundJobService backgroundJobService,
            CancellationToken cancellationToken) =>
        {
            var result = await backgroundJobService.RunMaintenanceAsync(cancellationToken);
            return Results.Ok(ApiResponse<BackgroundJobRunResult>.Ok(result, "Background maintenance completed"));
        });

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

        investors.MapGet("/me/dashboard", async (
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? timezoneOffsetMinutes,
            IDashboardService dashboardService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await dashboardService.GetInvestorDashboardAsync(
                httpContext.User,
                new DashboardQuery(from, to, timezoneOffsetMinutes ?? 0),
                cancellationToken);

            return Results.Ok(ApiResponse<InvestorDashboardDto>.Ok(result));
        })
        .WithTags("Dashboards");

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

        projects.MapGet("/{projectId:guid}/members", async (
            Guid projectId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.GetMembersAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectMemberDto>>.Ok(result));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/members/{memberId:guid}", async (
            Guid projectId,
            Guid memberId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.GetMemberAsync(httpContext.User, projectId, memberId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectMemberDto>.Ok(result));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPatch("/{projectId:guid}/members/{memberId:guid}", async (
            Guid projectId,
            Guid memberId,
            UpdateProjectMemberRequest request,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.UpdateMemberAsync(httpContext.User, projectId, memberId, request, cancellationToken);
            return Results.Ok(ApiResponse<ProjectMemberDto>.Ok(result, "Project member updated"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapDelete("/{projectId:guid}/members/{memberId:guid}", async (
            Guid projectId,
            Guid memberId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await teamService.RemoveMemberAsync(httpContext.User, projectId, memberId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Project member removed"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/invitations", async (
            Guid projectId,
            CreateProjectInvitationRequest request,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.CreateInvitationAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<ProjectInvitationDto>.Ok(result, "Project invitation created"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapGet("/{projectId:guid}/invitations", async (
            Guid projectId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.GetInvitationsAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyCollection<ProjectInvitationDto>>.Ok(result));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        api.MapPost("/project-invitations/{invitationId:guid}/accept", async (
            Guid invitationId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.AcceptInvitationAsync(httpContext.User, invitationId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectInvitationDto>.Ok(result, "Project invitation accepted"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        api.MapPost("/project-invitations/{invitationId:guid}/reject", async (
            Guid invitationId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.RejectInvitationAsync(httpContext.User, invitationId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectInvitationDto>.Ok(result, "Project invitation rejected"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        api.MapPost("/project-invitations/{invitationId:guid}/cancel", async (
            Guid invitationId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.CancelInvitationAsync(httpContext.User, invitationId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectInvitationDto>.Ok(result, "Project invitation cancelled"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/transfer-ownership", async (
            Guid projectId,
            TransferOwnershipRequest request,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.CreateOwnershipTransferAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<ProjectOwnershipTransferDto>.Ok(result, "Ownership transfer created"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/transfer-ownership/accept", async (
            Guid projectId,
            AcceptOwnershipTransferRequest request,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.AcceptOwnershipTransferAsync(httpContext.User, projectId, request, cancellationToken);
            return Results.Ok(ApiResponse<ProjectOwnershipTransferDto>.Ok(result, "Ownership transfer accepted"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/members/{memberId:guid}/promote-cofounder", async (
            Guid projectId,
            Guid memberId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.PromoteCoFounderAsync(httpContext.User, projectId, memberId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectMemberDto>.Ok(result, "Project member promoted to co-founder"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/members/{memberId:guid}/remove-cofounder", async (
            Guid projectId,
            Guid memberId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await teamService.RemoveCoFounderAsync(httpContext.User, projectId, memberId, cancellationToken);
            return Results.Ok(ApiResponse<ProjectMemberDto>.Ok(result, "Co-founder role removed"));
        })
        .WithTags("Project Team")
        .RequireAuthorization();

        projects.MapPost("/{projectId:guid}/leave", async (
            Guid projectId,
            IProjectTeamService teamService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            await teamService.LeaveAsync(httpContext.User, projectId, cancellationToken);
            return Results.Ok(ApiResponse<object>.Ok(null, "Left project successfully"));
        })
        .WithTags("Project Team")
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

    private static RefreshTokenRequest WithRefreshTokenCookieFallback(
        RefreshTokenRequest request,
        HttpContext httpContext,
        StartupConnectSecurityOptions securityOptions)
    {
        return string.IsNullOrWhiteSpace(request.RefreshToken)
            ? new RefreshTokenRequest(AuthCookieHelper.ReadRefreshTokenCookie(httpContext.Request, securityOptions) ?? string.Empty)
            : request;
    }

    private static LogoutRequest WithRefreshTokenCookieFallback(
        LogoutRequest request,
        HttpContext httpContext,
        StartupConnectSecurityOptions securityOptions)
    {
        return string.IsNullOrWhiteSpace(request.RefreshToken)
            ? new LogoutRequest(AuthCookieHelper.ReadRefreshTokenCookie(httpContext.Request, securityOptions) ?? string.Empty)
            : request;
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

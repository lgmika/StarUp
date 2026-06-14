using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Auth;
using System.Text;

namespace StartupConnect.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var admin = await SeedUserAsync(
            dbContext,
            passwordHasher,
            "admin@startupconnect.local",
            "Admin User",
            [SystemRoles.Admin, SystemRoles.VerifiedUser],
            cancellationToken);
        var moderator = await SeedUserAsync(
            dbContext,
            passwordHasher,
            "moderator@startupconnect.local",
            "Moderator User",
            [SystemRoles.Moderator, SystemRoles.VerifiedUser],
            cancellationToken);
        var investor = await SeedUserAsync(
            dbContext,
            passwordHasher,
            "investor@startupconnect.local",
            "Investor User",
            [SystemRoles.Investor, SystemRoles.VerifiedUser],
            cancellationToken);
        var business = await SeedUserAsync(
            dbContext,
            passwordHasher,
            "business@startupconnect.local",
            "Business User",
            [SystemRoles.Business, SystemRoles.VerifiedUser],
            cancellationToken);
        var verified = await SeedUserAsync(
            dbContext,
            passwordHasher,
            "verified@startupconnect.local",
            "Verified User",
            [SystemRoles.VerifiedUser],
            cancellationToken);

        await SeedDemoDataAsync(dbContext, configuration, admin, moderator, investor, business, verified, cancellationToken);
    }

    private static async Task<User> SeedUserAsync(
        AppDbContext dbContext,
        PasswordHasher passwordHasher,
        string email,
        string fullName,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await dbContext.Users
            .Include(item => item.UserRoles)
            .FirstOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                NormalizedEmail = normalizedEmail,
                FullName = fullName,
                PasswordHash = passwordHasher.Hash("Startup123!"),
                IsEmailVerified = true
            };
            dbContext.Users.Add(user);
        }

        var roles = await dbContext.Roles
            .Where(role => roleCodes.Contains(role.Code))
            .ToArrayAsync(cancellationToken);

        foreach (var role in roles)
        {
            if (!user.UserRoles.Any(userRole => userRole.RoleId == role.Id))
            {
                user.UserRoles.Add(new UserRole { User = user, RoleId = role.Id });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static async Task SeedDemoDataAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        User admin,
        User moderator,
        User investor,
        User business,
        User verified,
        CancellationToken cancellationToken)
    {
        await SeedProfilesAsync(dbContext, investor, business, verified, cancellationToken);
        var defaultCv = await SeedCvAsync(dbContext, configuration, verified, cancellationToken);
        var publishedProject = await SeedProjectAsync(
            dbContext,
            business,
            "demo-ai-matchmaking-platform",
            "AI Matchmaking Platform",
            ProjectStatus.Published,
            ProjectStage.MVP,
            ProjectVisibility.Public,
            cancellationToken);
        var pendingProject = await SeedProjectAsync(
            dbContext,
            business,
            "demo-green-logistics-hub",
            "Green Logistics Hub",
            ProjectStatus.PendingReview,
            ProjectStage.Prototype,
            ProjectVisibility.Limited,
            cancellationToken);
        var ndaProject = await SeedProjectAsync(
            dbContext,
            business,
            "demo-fintech-nda-vault",
            "Fintech NDA Vault",
            ProjectStatus.Published,
            ProjectStage.Beta,
            ProjectVisibility.NdaRequired,
            cancellationToken);

        await SeedApplicationFlowAsync(dbContext, business, verified, publishedProject, defaultCv, cancellationToken);
        await SeedInvestorFlowAsync(dbContext, investor, business, publishedProject, cancellationToken);
        await SeedNdaAsync(dbContext, investor, ndaProject, cancellationToken);
        await SeedReportsAsync(dbContext, moderator, verified, publishedProject, cancellationToken);
        await SeedNotificationsAsync(dbContext, admin, moderator, investor, business, verified, publishedProject, cancellationToken);
        await SeedSubscriptionsAsync(dbContext, investor, business, verified, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProfilesAsync(
        AppDbContext dbContext,
        User investor,
        User business,
        User verified,
        CancellationToken cancellationToken)
    {
        await UpsertProfileAsync(dbContext, business, "Founder building AI tools for startup teams", "I am validating collaboration tooling for early-stage founders.", "Ho Chi Minh City", cancellationToken);
        await UpsertProfileAsync(dbContext, verified, "Full-stack developer open to startup projects", "I build ASP.NET Core and Next.js products with strong product thinking.", "Da Nang", cancellationToken);

        if (!await dbContext.InvestorProfiles.AnyAsync(profile => profile.UserId == investor.Id, cancellationToken))
        {
            dbContext.InvestorProfiles.Add(new InvestorProfile
            {
                UserId = investor.Id,
                DisplayName = "Demo Investor",
                OrganizationName = "SeedBridge Capital",
                Bio = "Angel investor focused on B2B SaaS, AI tooling, and fintech.",
                InvestmentFocus = "AI SaaS, productivity, fintech infrastructure",
                WebsiteUrl = "https://startupconnect.local/investor",
                LinkedInUrl = "https://linkedin.com/company/seedbridge",
                MinTicketSize = 25_000,
                MaxTicketSize = 250_000
            });
        }

        var skills = await dbContext.Skills.ToArrayAsync(cancellationToken);
        await AddUserSkillAsync(dbContext, verified, skills, "Backend", 4, cancellationToken);
        await AddUserSkillAsync(dbContext, verified, skills, "Frontend", 3, cancellationToken);
        await AddUserSkillAsync(dbContext, business, skills, "Product Management", 5, cancellationToken);
        await AddUserSkillAsync(dbContext, business, skills, "Finance", 2, cancellationToken);
    }

    private static async Task UpsertProfileAsync(
        AppDbContext dbContext,
        User user,
        string headline,
        string bio,
        string location,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(item => item.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            dbContext.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                Headline = headline,
                Bio = bio,
                Location = location,
                LinkedInUrl = "https://linkedin.com/in/demo",
                WebsiteUrl = "https://startupconnect.local"
            });
            return;
        }

        profile.Headline = headline;
        profile.Bio = bio;
        profile.Location = location;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static async Task AddUserSkillAsync(
        AppDbContext dbContext,
        User user,
        IReadOnlyCollection<Skill> skills,
        string skillName,
        int years,
        CancellationToken cancellationToken)
    {
        var skill = skills.FirstOrDefault(item => item.Name == skillName);
        if (skill is null ||
            await dbContext.UserSkills.AnyAsync(item => item.UserId == user.Id && item.SkillId == skill.Id, cancellationToken))
        {
            return;
        }

        dbContext.UserSkills.Add(new UserSkill { UserId = user.Id, SkillId = skill.Id, YearsOfExperience = years });
    }

    private static async Task<Cv> SeedCvAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        User verified,
        CancellationToken cancellationToken)
    {
        var cv = await dbContext.CVs.FirstOrDefaultAsync(item => item.UserId == verified.Id && item.Title == "Demo Full-stack CV", cancellationToken);
        if (cv is not null)
        {
            return cv;
        }

        const string storagePath = "cvs/demo-fullstack-cv.pdf";
        var rootPath = configuration["FileStorage:LocalRootPath"] ?? "storage/private";
        var absolutePath = Path.GetFullPath(Path.Combine(rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        if (!File.Exists(absolutePath))
        {
            await File.WriteAllBytesAsync(absolutePath, Encoding.ASCII.GetBytes("%PDF-1.4\n% Demo StartupConnect CV\n"), cancellationToken);
        }

        var file = new StoredFile
        {
            OwnerUserId = verified.Id,
            OriginalFileName = "demo-fullstack-cv.pdf",
            StoredFileName = "demo-fullstack-cv.pdf",
            StoragePath = storagePath,
            ContentType = "application/pdf",
            SizeInBytes = new FileInfo(absolutePath).Length
        };
        dbContext.Files.Add(file);

        cv = new Cv
        {
            UserId = verified.Id,
            Title = "Demo Full-stack CV",
            Summary = "Demo CV used for frontend review and application flows.",
            Type = CvType.UploadedPdf,
            File = file,
            IsDefault = true
        };
        dbContext.CVs.Add(cv);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cv;
    }

    private static async Task<Project> SeedProjectAsync(
        AppDbContext dbContext,
        User owner,
        string slug,
        string title,
        ProjectStatus status,
        ProjectStage stage,
        ProjectVisibility visibility,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (project is null)
        {
            project = new Project
            {
                OwnerUserId = owner.Id,
                Title = title,
                Slug = slug,
                Summary = $"{title} demo project for StartupConnect frontend review.",
                Problem = "Early startup teams struggle to find trusted collaborators, investors, and structured review feedback.",
                Solution = "StartupConnect centralizes discovery, applications, investor interest, moderation, NDA, chat, and dashboard workflows.",
                TargetMarket = "Early-stage founders, operators, developers, and angel investors.",
                BusinessModel = "Freemium SaaS with subscription tiers and investor access.",
                FundingNeeds = "Seeking 150k USD seed capital for product and go-to-market.",
                PitchDeckUrl = "https://startupconnect.local/demo-pitch-deck.pdf",
                Status = status,
                Stage = stage,
                IsRecruiting = status == ProjectStatus.Published,
                SubmittedAt = status == ProjectStatus.PendingReview ? DateTimeOffset.UtcNow.AddDays(-1) : DateTimeOffset.UtcNow.AddDays(-10)
            };
            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.ProjectVisibilitySettings.AnyAsync(item => item.ProjectId == project.Id, cancellationToken))
        {
            dbContext.ProjectVisibilitySettings.Add(new ProjectVisibilitySetting
            {
                ProjectId = project.Id,
                Visibility = visibility,
                RequiresNda = visibility == ProjectVisibility.NdaRequired,
                ShowFounderContact = visibility is ProjectVisibility.Public or ProjectVisibility.Limited
            });
        }

        if (!await dbContext.ProjectMembers.AnyAsync(item => item.ProjectId == project.Id && item.UserId == owner.Id, cancellationToken))
        {
            dbContext.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = owner.Id,
                Role = ProjectMemberRole.Founder
            });
        }

        if (!await dbContext.ProjectRequiredRoles.AnyAsync(item => item.ProjectId == project.Id, cancellationToken))
        {
            dbContext.ProjectRequiredRoles.Add(new ProjectRequiredRole
            {
                ProjectId = project.Id,
                RoleName = "Full-stack Engineer",
                Description = "Build backend APIs and frontend workflows.",
                Slots = 2,
                IsOpen = true
            });
            dbContext.ProjectRequiredRoles.Add(new ProjectRequiredRole
            {
                ProjectId = project.Id,
                RoleName = "Growth Lead",
                Description = "Validate acquisition channels and investor narrative.",
                Slots = 1,
                IsOpen = true
            });
        }

        var backendSkill = await dbContext.Skills.FirstOrDefaultAsync(item => item.Name == "Backend", cancellationToken);
        if (backendSkill is not null &&
            !await dbContext.ProjectRequiredSkills.AnyAsync(item => item.ProjectId == project.Id && item.SkillId == backendSkill.Id, cancellationToken))
        {
            dbContext.ProjectRequiredSkills.Add(new ProjectRequiredSkill { ProjectId = project.Id, SkillId = backendSkill.Id });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    private static async Task SeedApplicationFlowAsync(
        AppDbContext dbContext,
        User founder,
        User applicant,
        Project project,
        Cv cv,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.ProjectApplications.FirstOrDefaultAsync(
            item => item.ProjectId == project.Id && item.ApplicantUserId == applicant.Id,
            cancellationToken);
        if (application is null)
        {
            application = new ProjectApplication
            {
                ProjectId = project.Id,
                ApplicantUserId = applicant.Id,
                CvId = cv.Id,
                CoverLetter = "I would like to join this project as a full-stack engineer and help ship the MVP.",
                Status = ApplicationStatus.Interviewing,
                FounderNote = "Strong technical profile. Schedule interview."
            };
            dbContext.ProjectApplications.Add(application);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                ApplicationId = application.Id,
                FromStatus = ApplicationStatus.Pending,
                ToStatus = ApplicationStatus.Interviewing,
                ChangedByUserId = founder.Id,
                Reason = "Demo interview scheduled"
            });
        }

        if (!await dbContext.ProjectInterviews.AnyAsync(item => item.ApplicationId == application.Id, cancellationToken))
        {
            var interview = new ProjectInterview
            {
                ApplicationId = application.Id,
                ProjectId = project.Id,
                ScheduledByUserId = founder.Id,
                StartAt = DateTimeOffset.UtcNow.AddDays(2).AddHours(3),
                EndAt = DateTimeOffset.UtcNow.AddDays(2).AddHours(4),
                TimeZone = "Asia/Ho_Chi_Minh",
                MeetingType = InterviewMeetingType.Online,
                MeetingUrl = "https://meet.google.com/demo-startupconnect",
                Note = "Demo interview for frontend calendar and application detail.",
                Status = InterviewStatus.Scheduled
            };
            dbContext.ProjectInterviews.Add(interview);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.InterviewParticipants.Add(new InterviewParticipant { InterviewId = interview.Id, UserId = founder.Id });
            dbContext.InterviewParticipants.Add(new InterviewParticipant { InterviewId = interview.Id, UserId = applicant.Id });
        }

        await SeedConversationAsync(dbContext, ConversationType.Application, project, application, null, founder, applicant, cancellationToken);
    }

    private static async Task SeedInvestorFlowAsync(
        AppDbContext dbContext,
        User investor,
        User founder,
        Project project,
        CancellationToken cancellationToken)
    {
        var interest = await dbContext.InvestorProjectInterests.FirstOrDefaultAsync(
            item => item.ProjectId == project.Id && item.InvestorUserId == investor.Id,
            cancellationToken);
        if (interest is null)
        {
            interest = new InvestorProjectInterest
            {
                ProjectId = project.Id,
                InvestorUserId = investor.Id,
                Message = "I am interested in this project. Please share traction metrics and current fundraising status.",
                Status = InvestorInterestStatus.Pending
            };
            dbContext.InvestorProjectInterests.Add(interest);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedConversationAsync(dbContext, ConversationType.Investor, project, null, interest, investor, founder, cancellationToken);
    }

    private static async Task SeedConversationAsync(
        AppDbContext dbContext,
        ConversationType type,
        Project project,
        ProjectApplication? application,
        InvestorProjectInterest? interest,
        User firstUser,
        User secondUser,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations.FirstOrDefaultAsync(
            item => item.Type == type &&
                item.ProjectId == project.Id &&
                item.ApplicationId == (application == null ? null : application.Id) &&
                item.InvestorInterestId == (interest == null ? null : interest.Id),
            cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                Type = type,
                ProjectId = project.Id,
                ApplicationId = application?.Id,
                InvestorInterestId = interest?.Id,
                Title = $"{project.Title} - {type}"
            };
            dbContext.Conversations.Add(conversation);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var user in new[] { firstUser, secondUser })
        {
            if (!await dbContext.ConversationParticipants.AnyAsync(item => item.ConversationId == conversation.Id && item.UserId == user.Id, cancellationToken))
            {
                dbContext.ConversationParticipants.Add(new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = user.Id,
                    LastReadAt = DateTimeOffset.UtcNow.AddMinutes(-20)
                });
            }
        }

        if (!await dbContext.Messages.AnyAsync(item => item.ConversationId == conversation.Id, cancellationToken))
        {
            dbContext.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderUserId = firstUser.Id,
                Content = $"Demo message from {firstUser.FullName} for {type} conversation."
            });
            dbContext.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderUserId = secondUser.Id,
                Content = "Thanks, this conversation is ready for frontend review."
            });
        }
    }

    private static async Task SeedNdaAsync(AppDbContext dbContext, User investor, Project project, CancellationToken cancellationToken)
    {
        var template = await dbContext.NdaTemplates.FirstOrDefaultAsync(item => item.Name == "Demo Mutual NDA", cancellationToken);
        if (template is null)
        {
            template = new NdaTemplate
            {
                Name = "Demo Mutual NDA",
                Description = "Demo NDA template for project access flows.",
                IsActive = true
            };
            dbContext.NdaTemplates.Add(template);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var version = await dbContext.NdaTemplateVersions.FirstOrDefaultAsync(
            item => item.TemplateId == template.Id && item.VersionNumber == 1,
            cancellationToken);
        if (version is null)
        {
            version = new NdaTemplateVersion
            {
                TemplateId = template.Id,
                VersionNumber = 1,
                Content = "Demo NDA content. Recipient agrees to keep project information confidential.",
                IsPublished = true
            };
            dbContext.NdaTemplateVersions.Add(version);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.NdaAgreements.AnyAsync(item => item.ProjectId == project.Id && item.UserId == investor.Id && item.TemplateVersionId == version.Id, cancellationToken))
        {
            dbContext.NdaAgreements.Add(new NdaAgreement
            {
                ProjectId = project.Id,
                UserId = investor.Id,
                TemplateId = template.Id,
                TemplateVersionId = version.Id,
                VersionNumber = version.VersionNumber,
                AgreementSnapshot = version.Content,
                IpAddress = "127.0.0.1",
                UserAgent = "StartupConnect Demo Seeder"
            });
        }
    }

    private static async Task SeedReportsAsync(
        AppDbContext dbContext,
        User moderator,
        User reporter,
        Project project,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Reports.FirstOrDefaultAsync(
            item => item.ReporterUserId == reporter.Id && item.TargetType == "Project" && item.TargetId == project.Id,
            cancellationToken);
        if (report is null)
        {
            report = new Report
            {
                ReporterUserId = reporter.Id,
                TargetType = "Project",
                TargetId = project.Id,
                ReasonCode = ReportReasonCode.FakeInformation,
                Description = "Demo report for moderator queue.",
                Reason = "FakeInformation: Demo report for moderator queue.",
                Status = ReportStatus.Investigating,
                AssignedModeratorId = moderator.Id
            };
            dbContext.Reports.Add(report);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ReportActions.Add(new ReportAction
            {
                ReportId = report.Id,
                ActorUserId = reporter.Id,
                Action = "Created",
                Reason = "Demo report submitted"
            });
            dbContext.ReportActions.Add(new ReportAction
            {
                ReportId = report.Id,
                ActorUserId = moderator.Id,
                Action = "Investigating",
                Reason = "Demo moderator investigation"
            });
        }
    }

    private static async Task SeedNotificationsAsync(
        AppDbContext dbContext,
        User admin,
        User moderator,
        User investor,
        User business,
        User verified,
        Project project,
        CancellationToken cancellationToken)
    {
        await AddNotificationAsync(dbContext, admin, NotificationType.System, "Demo database ready", "Seeded demo data is available for frontend review.", "System", null, "/admin", cancellationToken);
        await AddNotificationAsync(dbContext, moderator, NotificationType.Report, "Report needs review", "A demo report is waiting in the moderator queue.", "Project", project.Id, $"/moderator/reports", cancellationToken);
        await AddNotificationAsync(dbContext, investor, NotificationType.InvestorInterest, "Interest submitted", "Your demo investor interest is pending founder review.", "Project", project.Id, $"/investor/interests", cancellationToken);
        await AddNotificationAsync(dbContext, business, NotificationType.Application, "New application", "A demo candidate applied to your project.", "Project", project.Id, $"/projects/{project.Id}/applications", cancellationToken);
        await AddNotificationAsync(dbContext, verified, NotificationType.Interview, "Interview scheduled", "Your demo interview is scheduled in two days.", "Project", project.Id, "/applications", cancellationToken);
    }

    private static async Task AddNotificationAsync(
        AppDbContext dbContext,
        User user,
        NotificationType type,
        string title,
        string message,
        string? resourceType,
        Guid? resourceId,
        string? actionUrl,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Notifications.AnyAsync(item => item.UserId == user.Id && item.Title == title && !item.IsDeleted, cancellationToken))
        {
            return;
        }

        dbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Type = type,
            Title = title,
            Message = message,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ActionUrl = actionUrl
        });
    }

    private static async Task SeedSubscriptionsAsync(
        AppDbContext dbContext,
        User investor,
        User business,
        User verified,
        CancellationToken cancellationToken)
    {
        await AddSubscriptionAsync(dbContext, investor, "InvestorPro", SubscriptionStatus.Active, cancellationToken);
        await AddSubscriptionAsync(dbContext, business, "Business", SubscriptionStatus.Active, cancellationToken);
        await AddSubscriptionAsync(dbContext, verified, "Pro", SubscriptionStatus.Trialing, cancellationToken);
    }

    private static async Task AddSubscriptionAsync(
        AppDbContext dbContext,
        User user,
        string planCode,
        SubscriptionStatus status,
        CancellationToken cancellationToken)
    {
        if (await dbContext.UserSubscriptions.AnyAsync(item => item.UserId == user.Id, cancellationToken))
        {
            return;
        }

        var plan = await dbContext.SubscriptionPlans.FirstOrDefaultAsync(item => item.Code == planCode, cancellationToken);
        if (plan is null)
        {
            return;
        }

        dbContext.UserSubscriptions.Add(new UserSubscription
        {
            UserId = user.Id,
            PlanId = plan.Id,
            Status = status,
            Provider = "Demo",
            ProviderSubscriptionId = $"demo_{user.Id:N}",
            CurrentPeriodStart = DateTimeOffset.UtcNow.AddDays(-7),
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddDays(23),
            TrialEndsAt = status == SubscriptionStatus.Trialing ? DateTimeOffset.UtcNow.AddDays(7) : null
        });
    }
}

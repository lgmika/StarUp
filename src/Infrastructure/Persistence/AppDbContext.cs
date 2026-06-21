using Microsoft.EntityFrameworkCore;
using StartupConnect.Domain.Entities;

namespace StartupConnect.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BackgroundJobExecution> BackgroundJobExecutions => Set<BackgroundJobExecution>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<UserSkill> UserSkills => Set<UserSkill>();

    public DbSet<Cv> CVs => Set<Cv>();

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<StoredFile> Files => Set<StoredFile>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectView> ProjectViews => Set<ProjectView>();

    public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();

    public DbSet<ProjectVisibilitySetting> ProjectVisibilitySettings => Set<ProjectVisibilitySetting>();

    public DbSet<ProjectRequiredRole> ProjectRequiredRoles => Set<ProjectRequiredRole>();

    public DbSet<ProjectRequiredSkill> ProjectRequiredSkills => Set<ProjectRequiredSkill>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<ProjectInvitation> ProjectInvitations => Set<ProjectInvitation>();

    public DbSet<ProjectOwnershipTransfer> ProjectOwnershipTransfers => Set<ProjectOwnershipTransfer>();

    public DbSet<ProjectMemberHistory> ProjectMemberHistories => Set<ProjectMemberHistory>();

    public DbSet<ProjectAccessGrant> ProjectAccessGrants => Set<ProjectAccessGrant>();

    public DbSet<SavedProject> SavedProjects => Set<SavedProject>();

    public DbSet<AIRequest> AIRequests => Set<AIRequest>();

    public DbSet<AIReview> AIReviews => Set<AIReview>();

    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();

    public DbSet<ProjectModerationReview> ProjectModerationReviews => Set<ProjectModerationReview>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ReportAction> ReportActions => Set<ReportAction>();

    public DbSet<RecommendationDismissal> RecommendationDismissals => Set<RecommendationDismissal>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();

    public DbSet<UsageQuota> UsageQuotas => Set<UsageQuota>();

    public DbSet<ProjectApplication> ProjectApplications => Set<ProjectApplication>();

    public DbSet<ApplicationAttachment> ApplicationAttachments => Set<ApplicationAttachment>();

    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();

    public DbSet<ProjectInterview> ProjectInterviews => Set<ProjectInterview>();

    public DbSet<InterviewParticipant> InterviewParticipants => Set<InterviewParticipant>();

    public DbSet<InterviewStatusHistory> InterviewStatusHistories => Set<InterviewStatusHistory>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    public DbSet<MessageReadReceipt> MessageReadReceipts => Set<MessageReadReceipt>();

    public DbSet<InvestorProfile> InvestorProfiles => Set<InvestorProfile>();

    public DbSet<InvestorProjectInterest> InvestorProjectInterests => Set<InvestorProjectInterest>();

    public DbSet<NdaTemplate> NdaTemplates => Set<NdaTemplate>();

    public DbSet<NdaTemplateVersion> NdaTemplateVersions => Set<NdaTemplateVersion>();

    public DbSet<NdaAgreement> NdaAgreements => Set<NdaAgreement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BackgroundJobExecution>(entity =>
        {
            entity.ToTable("background_job_executions");
            entity.HasKey(execution => execution.Id);
            entity.HasIndex(execution => new { execution.JobName, execution.StartedAt });
            entity.HasIndex(execution => new { execution.Status, execution.StartedAt });
            entity.Property(execution => execution.JobName).HasMaxLength(160).IsRequired();
            entity.Property(execution => execution.Error).HasMaxLength(1000);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(setting => setting.Id);
            entity.HasIndex(setting => setting.Key).IsUnique();
            entity.HasIndex(setting => setting.Group);
            entity.Property(setting => setting.Key).HasMaxLength(160).IsRequired();
            entity.Property(setting => setting.Group).HasMaxLength(80).IsRequired();
            entity.Property(setting => setting.Value).HasMaxLength(2000).IsRequired();
            entity.Property(setting => setting.Type).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");
            entity.HasKey(activity => activity.Id);
            entity.HasIndex(activity => new { activity.ProjectId, activity.CreatedAt });
            entity.HasIndex(activity => new { activity.Visibility, activity.CreatedAt });
            entity.Property(activity => activity.Title).HasMaxLength(180).IsRequired();
            entity.Property(activity => activity.Message).HasMaxLength(1000);
            entity.Property(activity => activity.TargetType).HasMaxLength(120);
            entity.Property(activity => activity.MetadataJson).HasColumnType("jsonb");

            entity.HasOne(activity => activity.Project)
                .WithMany()
                .HasForeignKey(activity => activity.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(activity => activity.ActorUser)
                .WithMany()
                .HasForeignKey(activity => activity.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(auditLog => auditLog.Id);

            entity.Property(auditLog => auditLog.Action).HasMaxLength(120).IsRequired();
            entity.Property(auditLog => auditLog.ResourceType).HasMaxLength(120).IsRequired();
            entity.Property(auditLog => auditLog.Reason).HasMaxLength(500);
            entity.Property(auditLog => auditLog.IpAddress).HasMaxLength(64);
            entity.Property(auditLog => auditLog.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);

            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.SuspensionReason).HasMaxLength(1000);
            entity.Property(user => user.BanReason).HasMaxLength(1000);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);

            entity.HasIndex(role => role.Code).IsUnique();
            entity.Property(role => role.Code).HasMaxLength(60).IsRequired();
            entity.Property(role => role.Name).HasMaxLength(120).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(300);

            entity.HasData(
                CreateRole("11111111-1111-1111-1111-111111111111", "Guest", "Guest", "Anonymous visitor role"),
                CreateRole("22222222-2222-2222-2222-222222222222", "User", "User", "Registered user"),
                CreateRole("33333333-3333-3333-3333-333333333333", "VerifiedUser", "Verified User", "Email verified user"),
                CreateRole("44444444-4444-4444-4444-444444444444", "Business", "Business", "Business account"),
                CreateRole("55555555-5555-5555-5555-555555555555", "Investor", "Investor", "Investor account"),
                CreateRole("66666666-6666-6666-6666-666666666666", "Moderator", "Moderator", "Moderation role"),
                CreateRole("77777777-7777-7777-7777-777777777777", "Admin", "Admin", "System admin"));
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });

            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(refreshToken => refreshToken.Id);
            entity.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();

            entity.Property(refreshToken => refreshToken.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(refreshToken => refreshToken.ReplacedByTokenHash).HasMaxLength(128);
            entity.Property(refreshToken => refreshToken.RevokedAt).IsConcurrencyToken();
            entity.Property(refreshToken => refreshToken.CreatedByIp).HasMaxLength(64);
            entity.Property(refreshToken => refreshToken.RevokedByIp).HasMaxLength(64);

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("email_verification_tokens");
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();

            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();

            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();

            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.ToTable("email_outbox_messages");
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => new { message.SentAt, message.NextAttemptAt, message.LockedUntil });
            entity.Property(message => message.Recipient).HasMaxLength(255).IsRequired();
            entity.Property(message => message.Template).HasMaxLength(80).IsRequired();
            entity.Property(message => message.ProtectedPayload).HasMaxLength(12000).IsRequired();
            entity.Property(message => message.LastError).HasMaxLength(1000);
            entity.Property(message => message.LeaseId).IsConcurrencyToken();

            entity.HasOne(message => message.User)
                .WithMany()
                .HasForeignKey(message => message.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.Property(profile => profile.Headline).HasMaxLength(160).IsRequired();
            entity.Property(profile => profile.Bio).HasMaxLength(2000).IsRequired();
            entity.Property(profile => profile.Location).HasMaxLength(160);
            entity.Property(profile => profile.PhoneNumber).HasMaxLength(40);
            entity.Property(profile => profile.LinkedInUrl).HasMaxLength(500);
            entity.Property(profile => profile.GitHubUrl).HasMaxLength(500);
            entity.Property(profile => profile.WebsiteUrl).HasMaxLength(500);

            entity.HasOne(profile => profile.User)
                .WithOne()
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");
            entity.HasKey(skill => skill.Id);
            entity.HasIndex(skill => skill.NormalizedName).IsUnique();
            entity.Property(skill => skill.Name).HasMaxLength(120).IsRequired();
            entity.Property(skill => skill.NormalizedName).HasMaxLength(120).IsRequired();

            entity.HasData(
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", "Backend", "BACKEND"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2", "Frontend", "FRONTEND"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3", "Product Management", "PRODUCT MANAGEMENT"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4", "UI/UX Design", "UI/UX DESIGN"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5", "Marketing", "MARKETING"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6", "Sales", "SALES"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7", "Finance", "FINANCE"),
                CreateSkill("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8", "AI/ML", "AI/ML"));
        });

        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.ToTable("user_skills");
            entity.HasKey(userSkill => new { userSkill.UserId, userSkill.SkillId });

            entity.HasOne(userSkill => userSkill.User)
                .WithMany()
                .HasForeignKey(userSkill => userSkill.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(userSkill => userSkill.Skill)
                .WithMany(skill => skill.UserSkills)
                .HasForeignKey(userSkill => userSkill.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("files");
            entity.HasKey(file => file.Id);
            entity.Property(file => file.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(file => file.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(file => file.ContentType).HasMaxLength(120).IsRequired();

            entity.HasOne(file => file.OwnerUser)
                .WithMany()
                .HasForeignKey(file => file.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cv>(entity =>
        {
            entity.ToTable("cvs");
            entity.HasKey(cv => cv.Id);
            entity.HasIndex(cv => new { cv.UserId, cv.IsDefault });
            entity.Property(cv => cv.Title).HasMaxLength(160).IsRequired();
            entity.Property(cv => cv.Summary).HasMaxLength(2000);

            entity.HasOne(cv => cv.User)
                .WithMany()
                .HasForeignKey(cv => cv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cv => cv.File)
                .WithMany()
                .HasForeignKey(cv => cv.FileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("portfolios");
            entity.HasKey(portfolio => portfolio.Id);
            entity.Property(portfolio => portfolio.Title).HasMaxLength(160).IsRequired();
            entity.Property(portfolio => portfolio.Url).HasMaxLength(500).IsRequired();
            entity.Property(portfolio => portfolio.Description).HasMaxLength(1000);

            entity.HasOne(portfolio => portfolio.User)
                .WithMany()
                .HasForeignKey(portfolio => portfolio.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(project => project.Id);
            entity.HasIndex(project => project.Slug).IsUnique();
            entity.HasIndex(project => new { project.Status, project.IsDeleted });
            entity.Property(project => project.Title).HasMaxLength(180).IsRequired();
            entity.Property(project => project.Slug).HasMaxLength(220).IsRequired();
            entity.Property(project => project.Summary).HasMaxLength(1000).IsRequired();
            entity.Property(project => project.Problem).HasMaxLength(3000).IsRequired();
            entity.Property(project => project.Solution).HasMaxLength(3000).IsRequired();
            entity.Property(project => project.TargetMarket).HasMaxLength(1000);
            entity.Property(project => project.BusinessModel).HasMaxLength(1000);
            entity.Property(project => project.FundingNeeds).HasMaxLength(1000);
            entity.Property(project => project.PitchDeckUrl).HasMaxLength(500);

            entity.HasOne(project => project.OwnerUser)
                .WithMany()
                .HasForeignKey(project => project.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectVisibilitySetting>(entity =>
        {
            entity.ToTable("project_visibility_settings");
            entity.HasKey(setting => setting.Id);
            entity.HasIndex(setting => setting.ProjectId).IsUnique();

            entity.HasOne(setting => setting.Project)
                .WithOne()
                .HasForeignKey<ProjectVisibilitySetting>(setting => setting.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectVersion>(entity =>
        {
            entity.ToTable("project_versions");
            entity.HasKey(version => version.Id);
            entity.HasIndex(version => new { version.ProjectId, version.VersionNumber }).IsUnique();
            entity.Property(version => version.SnapshotJson).HasColumnType("jsonb").IsRequired();
            entity.Property(version => version.ChangeReason).HasMaxLength(300).IsRequired();

            entity.HasOne(version => version.Project)
                .WithMany()
                .HasForeignKey(version => version.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(version => version.ChangedByUser)
                .WithMany()
                .HasForeignKey(version => version.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectRequiredRole>(entity =>
        {
            entity.ToTable("project_required_roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.RoleName).HasMaxLength(120).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(1000);

            entity.HasOne(role => role.Project)
                .WithMany()
                .HasForeignKey(role => role.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectRequiredSkill>(entity =>
        {
            entity.ToTable("project_required_skills");
            entity.HasKey(requiredSkill => new { requiredSkill.ProjectId, requiredSkill.SkillId });

            entity.HasOne(requiredSkill => requiredSkill.Project)
                .WithMany()
                .HasForeignKey(requiredSkill => requiredSkill.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(requiredSkill => requiredSkill.Skill)
                .WithMany()
                .HasForeignKey(requiredSkill => requiredSkill.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("project_members");
            entity.HasKey(member => member.Id);
            entity.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique();

            entity.HasOne(member => member.Project)
                .WithMany(project => project.Members)
                .HasForeignKey(member => member.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(member => member.User)
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectInvitation>(entity =>
        {
            entity.ToTable("project_invitations");
            entity.HasKey(invitation => invitation.Id);
            entity.HasIndex(invitation => new { invitation.ProjectId, invitation.Email, invitation.Status });
            entity.Property(invitation => invitation.Email).HasMaxLength(255).IsRequired();
            entity.Property(invitation => invitation.Message).HasMaxLength(1000);

            entity.HasOne(invitation => invitation.Project)
                .WithMany()
                .HasForeignKey(invitation => invitation.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(invitation => invitation.InvitedByUser)
                .WithMany()
                .HasForeignKey(invitation => invitation.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(invitation => invitation.InvitedUser)
                .WithMany()
                .HasForeignKey(invitation => invitation.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectOwnershipTransfer>(entity =>
        {
            entity.ToTable("project_ownership_transfers");
            entity.HasKey(transfer => transfer.Id);
            entity.HasIndex(transfer => transfer.TokenHash).IsUnique();
            entity.Property(transfer => transfer.TokenHash).HasMaxLength(128).IsRequired();

            entity.HasOne(transfer => transfer.Project)
                .WithMany()
                .HasForeignKey(transfer => transfer.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(transfer => transfer.FromUser)
                .WithMany()
                .HasForeignKey(transfer => transfer.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(transfer => transfer.ToUser)
                .WithMany()
                .HasForeignKey(transfer => transfer.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectMemberHistory>(entity =>
        {
            entity.ToTable("project_member_histories");
            entity.HasKey(history => history.Id);
            entity.HasIndex(history => new { history.ProjectId, history.UserId, history.CreatedAt });
            entity.Property(history => history.Action).HasMaxLength(120).IsRequired();
            entity.Property(history => history.Reason).HasMaxLength(1000);
        });

        modelBuilder.Entity<ProjectAccessGrant>(entity =>
        {
            entity.ToTable("project_access_grants");
            entity.HasKey(grant => grant.Id);
            entity.HasIndex(grant => new { grant.ProjectId, grant.UserId }).IsUnique();
            entity.Property(grant => grant.AccessLevel).HasMaxLength(60).IsRequired();

            entity.HasOne(grant => grant.Project)
                .WithMany()
                .HasForeignKey(grant => grant.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(grant => grant.User)
                .WithMany()
                .HasForeignKey(grant => grant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SavedProject>(entity =>
        {
            entity.ToTable("saved_projects");
            entity.HasKey(saved => new { saved.UserId, saved.ProjectId });

            entity.HasOne(saved => saved.User)
                .WithMany()
                .HasForeignKey(saved => saved.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(saved => saved.Project)
                .WithMany()
                .HasForeignKey(saved => saved.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectView>(entity =>
        {
            entity.ToTable("project_views", table => table.HasCheckConstraint(
                "CK_project_views_viewer",
                "(\"ViewerUserId\" IS NOT NULL AND \"VisitorId\" IS NULL) OR (\"ViewerUserId\" IS NULL AND \"VisitorId\" IS NOT NULL)"));
            entity.HasKey(view => view.Id);
            entity.HasIndex(view => new { view.ProjectId, view.ViewerUserId, view.ViewedOn }).IsUnique();
            entity.HasIndex(view => new { view.ProjectId, view.VisitorId, view.ViewedOn }).IsUnique();
            entity.HasIndex(view => new { view.ProjectId, view.CreatedAt });

            entity.HasOne(view => view.Project)
                .WithMany()
                .HasForeignKey(view => view.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(view => view.ViewerUser)
                .WithMany()
                .HasForeignKey(view => view.ViewerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AIRequest>(entity =>
        {
            entity.ToTable("ai_requests");
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.UserId, request.CreatedAt });
            entity.Property(request => request.PromptSnapshot).HasMaxLength(4000).IsRequired();
            entity.Property(request => request.ResponseSnapshot).HasColumnType("text");
            entity.Property(request => request.Provider).HasMaxLength(80).IsRequired();

            entity.HasOne(request => request.User)
                .WithMany()
                .HasForeignKey(request => request.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.Project)
                .WithMany()
                .HasForeignKey(request => request.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIReview>(entity =>
        {
            entity.ToTable("ai_reviews");
            entity.HasKey(review => review.Id);
            entity.HasIndex(review => new { review.ProjectId, review.CreatedAt });
            entity.Property(review => review.MissingInformationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(review => review.RiskFlagsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(review => review.SuggestionsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(review => review.Summary).HasMaxLength(2000).IsRequired();

            entity.HasOne(review => review.Project)
                .WithMany()
                .HasForeignKey(review => review.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(review => review.RequestedByUser)
                .WithMany()
                .HasForeignKey(review => review.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(review => review.AIRequest)
                .WithMany()
                .HasForeignKey(review => review.AIRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIRecommendation>(entity =>
        {
            entity.ToTable("ai_recommendations");
            entity.HasKey(recommendation => recommendation.Id);
            entity.Property(recommendation => recommendation.Title).HasMaxLength(180).IsRequired();
            entity.Property(recommendation => recommendation.Content).HasMaxLength(3000).IsRequired();
            entity.Property(recommendation => recommendation.TargetField).HasMaxLength(80).IsRequired();

            entity.HasOne(recommendation => recommendation.Project)
                .WithMany()
                .HasForeignKey(recommendation => recommendation.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(recommendation => recommendation.RequestedByUser)
                .WithMany()
                .HasForeignKey(recommendation => recommendation.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(recommendation => recommendation.AIRequest)
                .WithMany()
                .HasForeignKey(recommendation => recommendation.AIRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectModerationReview>(entity =>
        {
            entity.ToTable("project_moderation_reviews");
            entity.HasKey(review => review.Id);
            entity.HasIndex(review => new { review.ProjectId, review.CreatedAt });
            entity.Property(review => review.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(review => review.AIRiskFlagsSnapshotJson).HasColumnType("jsonb");

            entity.HasOne(review => review.Project)
                .WithMany()
                .HasForeignKey(review => review.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(review => review.ModeratorUser)
                .WithMany()
                .HasForeignKey(review => review.ModeratorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.HasIndex(notification => new { notification.UserId, notification.ReadAt, notification.IsDeleted });
            entity.Property(notification => notification.Title).HasMaxLength(180).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(1000).IsRequired();
            entity.Property(notification => notification.ResourceType).HasMaxLength(80);
            entity.Property(notification => notification.ActionUrl).HasMaxLength(500);

            entity.HasOne(notification => notification.User)
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports");
            entity.HasKey(report => report.Id);
            entity.HasIndex(report => new { report.Status, report.CreatedAt });
            entity.Property(report => report.TargetType).HasMaxLength(80).IsRequired();
            entity.Property(report => report.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(report => report.Description).HasMaxLength(2000).IsRequired();
            entity.Property(report => report.Evidence).HasMaxLength(2000);
            entity.Property(report => report.Resolution).HasMaxLength(2000);

            entity.HasOne(report => report.ReporterUser)
                .WithMany()
                .HasForeignKey(report => report.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(report => report.AssignedModerator)
                .WithMany()
                .HasForeignKey(report => report.AssignedModeratorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReportAction>(entity =>
        {
            entity.ToTable("report_actions");
            entity.HasKey(action => action.Id);
            entity.Property(action => action.Action).HasMaxLength(120).IsRequired();
            entity.Property(action => action.Reason).HasMaxLength(1000).IsRequired();

            entity.HasOne(action => action.Report)
                .WithMany()
                .HasForeignKey(action => action.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(action => action.ActorUser)
                .WithMany()
                .HasForeignKey(action => action.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecommendationDismissal>(entity =>
        {
            entity.ToTable("recommendation_dismissals");
            entity.HasKey(dismissal => dismissal.Id);
            entity.HasIndex(dismissal => new { dismissal.UserId, dismissal.RecommendationId }).IsUnique();
            entity.HasIndex(dismissal => new { dismissal.UserId, dismissal.Type });

            entity.HasOne(dismissal => dismissal.User)
                .WithMany()
                .HasForeignKey(dismissal => dismissal.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("subscription_plans");
            entity.HasKey(plan => plan.Id);
            entity.HasIndex(plan => plan.Code).IsUnique();
            entity.Property(plan => plan.Code).HasMaxLength(80).IsRequired();
            entity.Property(plan => plan.Name).HasMaxLength(120).IsRequired();
            entity.Property(plan => plan.Description).HasMaxLength(500).IsRequired();
            entity.Property(plan => plan.MonthlyPrice).HasPrecision(12, 2);
            entity.Property(plan => plan.Currency).HasMaxLength(3).IsRequired();

            entity.HasData(
                CreatePlan("10000000-0000-0000-0000-000000000001", "Free", "Free", "Basic access for early users", 0),
                CreatePlan("10000000-0000-0000-0000-000000000002", "Pro", "Pro", "More AI requests, projects, and storage", 19),
                CreatePlan("10000000-0000-0000-0000-000000000003", "InvestorPro", "Investor Pro", "Investor access and advanced discovery", 49),
                CreatePlan("10000000-0000-0000-0000-000000000004", "Business", "Business", "Business collaboration and analytics", 99));
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.ToTable("user_subscriptions");
            entity.HasKey(subscription => subscription.Id);
            entity.HasIndex(subscription => new { subscription.UserId, subscription.Status });
            entity.HasIndex(subscription => subscription.ProviderSubscriptionId);
            entity.Property(subscription => subscription.Provider).HasMaxLength(80).IsRequired();
            entity.Property(subscription => subscription.ProviderSubscriptionId).HasMaxLength(200);

            entity.HasOne(subscription => subscription.User)
                .WithMany()
                .HasForeignKey(subscription => subscription.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(subscription => subscription.Plan)
                .WithMany()
                .HasForeignKey(subscription => subscription.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("payment_transactions");
            entity.HasKey(transaction => transaction.Id);
            entity.HasIndex(transaction => transaction.ProviderCheckoutSessionId);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.Status });
            entity.Property(transaction => transaction.Provider).HasMaxLength(80).IsRequired();
            entity.Property(transaction => transaction.ProviderCheckoutSessionId).HasMaxLength(200).IsRequired();
            entity.Property(transaction => transaction.ProviderTransactionId).HasMaxLength(200);
            entity.Property(transaction => transaction.Amount).HasPrecision(12, 2);
            entity.Property(transaction => transaction.Currency).HasMaxLength(3).IsRequired();

            entity.HasOne(transaction => transaction.User)
                .WithMany()
                .HasForeignKey(transaction => transaction.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(transaction => transaction.Plan)
                .WithMany()
                .HasForeignKey(transaction => transaction.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(transaction => transaction.Subscription)
                .WithMany()
                .HasForeignKey(transaction => transaction.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentWebhookEvent>(entity =>
        {
            entity.ToTable("payment_webhook_events");
            entity.HasKey(webhook => webhook.Id);
            entity.HasIndex(webhook => new { webhook.Provider, webhook.ProviderEventId }).IsUnique();
            entity.Property(webhook => webhook.Provider).HasMaxLength(80).IsRequired();
            entity.Property(webhook => webhook.ProviderEventId).HasMaxLength(200).IsRequired();
            entity.Property(webhook => webhook.EventType).HasMaxLength(120).IsRequired();
            entity.Property(webhook => webhook.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(webhook => webhook.ProcessingError).HasMaxLength(1000);
        });

        modelBuilder.Entity<UsageQuota>(entity =>
        {
            entity.ToTable("usage_quotas");
            entity.HasKey(quota => quota.Id);
            entity.HasIndex(quota => new { quota.PlanId, quota.ResourceKey }).IsUnique();
            entity.Property(quota => quota.ResourceKey).HasMaxLength(120).IsRequired();

            entity.HasOne(quota => quota.Plan)
                .WithMany()
                .HasForeignKey(quota => quota.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                CreateQuota("20000000-0000-0000-0000-000000000001", "10000000-0000-0000-0000-000000000001", "ai_requests_monthly", 20),
                CreateQuota("20000000-0000-0000-0000-000000000002", "10000000-0000-0000-0000-000000000001", "active_projects", 2),
                CreateQuota("20000000-0000-0000-0000-000000000003", "10000000-0000-0000-0000-000000000001", "file_storage_mb", 100),
                CreateQuota("20000000-0000-0000-0000-000000000004", "10000000-0000-0000-0000-000000000002", "ai_requests_monthly", 200),
                CreateQuota("20000000-0000-0000-0000-000000000005", "10000000-0000-0000-0000-000000000002", "active_projects", 10),
                CreateQuota("20000000-0000-0000-0000-000000000006", "10000000-0000-0000-0000-000000000002", "file_storage_mb", 2048),
                CreateQuota("20000000-0000-0000-0000-000000000007", "10000000-0000-0000-0000-000000000003", "investor_access", 1),
                CreateQuota("20000000-0000-0000-0000-000000000008", "10000000-0000-0000-0000-000000000003", "advanced_analytics", 1),
                CreateQuota("20000000-0000-0000-0000-000000000009", "10000000-0000-0000-0000-000000000004", "active_projects", 50),
                CreateQuota("20000000-0000-0000-0000-000000000010", "10000000-0000-0000-0000-000000000004", "file_storage_mb", 10240),
                CreateQuota("20000000-0000-0000-0000-000000000011", "10000000-0000-0000-0000-000000000004", "advanced_analytics", 1));
        });

        modelBuilder.Entity<ProjectApplication>(entity =>
        {
            entity.ToTable("project_applications");
            entity.HasKey(application => application.Id);
            entity.HasIndex(application => new { application.ProjectId, application.ApplicantUserId }).IsUnique();
            entity.HasIndex(application => new { application.ApplicantUserId, application.Status });
            entity.Property(application => application.CoverLetter).HasMaxLength(3000).IsRequired();
            entity.Property(application => application.FounderNote).HasMaxLength(1000);

            entity.HasOne(application => application.Project)
                .WithMany()
                .HasForeignKey(application => application.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(application => application.ApplicantUser)
                .WithMany()
                .HasForeignKey(application => application.ApplicantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(application => application.Cv)
                .WithMany()
                .HasForeignKey(application => application.CvId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationAttachment>(entity =>
        {
            entity.ToTable("application_attachments");
            entity.HasKey(attachment => attachment.Id);

            entity.HasOne(attachment => attachment.Application)
                .WithMany()
                .HasForeignKey(attachment => attachment.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(attachment => attachment.File)
                .WithMany()
                .HasForeignKey(attachment => attachment.FileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.ToTable("application_status_histories");
            entity.HasKey(history => history.Id);
            entity.HasIndex(history => new { history.ApplicationId, history.CreatedAt });
            entity.Property(history => history.Reason).HasMaxLength(1000);

            entity.HasOne(history => history.Application)
                .WithMany()
                .HasForeignKey(history => history.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(history => history.ChangedByUser)
                .WithMany()
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectInterview>(entity =>
        {
            entity.ToTable("project_interviews");
            entity.HasKey(interview => interview.Id);
            entity.HasIndex(interview => new { interview.ApplicationId, interview.StartAt });
            entity.HasIndex(interview => new { interview.ProjectId, interview.StartAt });
            entity.Property(interview => interview.TimeZone).HasMaxLength(120).IsRequired();
            entity.Property(interview => interview.MeetingUrl).HasMaxLength(1000);
            entity.Property(interview => interview.Location).HasMaxLength(500);
            entity.Property(interview => interview.Note).HasMaxLength(2000);
            entity.Property(interview => interview.CancellationReason).HasMaxLength(1000);

            entity.HasOne(interview => interview.Application)
                .WithMany()
                .HasForeignKey(interview => interview.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(interview => interview.Project)
                .WithMany()
                .HasForeignKey(interview => interview.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(interview => interview.ScheduledByUser)
                .WithMany()
                .HasForeignKey(interview => interview.ScheduledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InterviewParticipant>(entity =>
        {
            entity.ToTable("interview_participants");
            entity.HasKey(participant => participant.Id);
            entity.HasIndex(participant => new { participant.InterviewId, participant.UserId }).IsUnique();

            entity.HasOne(participant => participant.Interview)
                .WithMany()
                .HasForeignKey(participant => participant.InterviewId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InterviewStatusHistory>(entity =>
        {
            entity.ToTable("interview_status_histories");
            entity.HasKey(history => history.Id);
            entity.HasIndex(history => new { history.InterviewId, history.CreatedAt });
            entity.Property(history => history.Reason).HasMaxLength(1000);

            entity.HasOne(history => history.Interview)
                .WithMany()
                .HasForeignKey(history => history.InterviewId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(history => history.ChangedByUser)
                .WithMany()
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(conversation => conversation.Id);
            entity.HasIndex(conversation => new { conversation.Type, conversation.ProjectId, conversation.ApplicationId, conversation.InvestorInterestId });
            entity.Property(conversation => conversation.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("conversation_participants");
            entity.HasKey(participant => participant.Id);
            entity.HasIndex(participant => new { participant.ConversationId, participant.UserId }).IsUnique();
            entity.HasIndex(participant => new { participant.UserId, participant.ConversationId });

            entity.HasOne(participant => participant.Conversation)
                .WithMany()
                .HasForeignKey(participant => participant.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(participant => participant.User)
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(message => message.Id);
            entity.HasIndex(message => new { message.ConversationId, message.CreatedAt });
            entity.HasIndex(message => new { message.SenderUserId, message.CreatedAt });
            entity.Property(message => message.Content).HasMaxLength(4000).IsRequired();

            entity.HasOne(message => message.Conversation)
                .WithMany()
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.SenderUser)
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MessageAttachment>(entity =>
        {
            entity.ToTable("message_attachments");
            entity.HasKey(attachment => attachment.Id);
            entity.HasIndex(attachment => new { attachment.MessageId, attachment.FileId }).IsUnique();

            entity.HasOne(attachment => attachment.Message)
                .WithMany()
                .HasForeignKey(attachment => attachment.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(attachment => attachment.File)
                .WithMany()
                .HasForeignKey(attachment => attachment.FileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MessageReadReceipt>(entity =>
        {
            entity.ToTable("message_read_receipts");
            entity.HasKey(receipt => receipt.Id);
            entity.HasIndex(receipt => new { receipt.MessageId, receipt.UserId }).IsUnique();

            entity.HasOne(receipt => receipt.Message)
                .WithMany()
                .HasForeignKey(receipt => receipt.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(receipt => receipt.User)
                .WithMany()
                .HasForeignKey(receipt => receipt.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvestorProfile>(entity =>
        {
            entity.ToTable("investor_profiles");
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.Property(profile => profile.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(profile => profile.OrganizationName).HasMaxLength(180);
            entity.Property(profile => profile.Bio).HasMaxLength(2000);
            entity.Property(profile => profile.InvestmentFocus).HasMaxLength(1000);
            entity.Property(profile => profile.WebsiteUrl).HasMaxLength(500);
            entity.Property(profile => profile.LinkedInUrl).HasMaxLength(500);
            entity.Property(profile => profile.MinTicketSize).HasPrecision(18, 2);
            entity.Property(profile => profile.MaxTicketSize).HasPrecision(18, 2);

            entity.HasOne(profile => profile.User)
                .WithMany()
                .HasForeignKey(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvestorProjectInterest>(entity =>
        {
            entity.ToTable("investor_project_interests");
            entity.HasKey(interest => interest.Id);
            entity.HasIndex(interest => new { interest.ProjectId, interest.InvestorUserId }).IsUnique();
            entity.HasIndex(interest => new { interest.InvestorUserId, interest.Status });
            entity.Property(interest => interest.Message).HasMaxLength(2000).IsRequired();
            entity.Property(interest => interest.FounderResponse).HasMaxLength(1000);

            entity.HasOne(interest => interest.Project)
                .WithMany()
                .HasForeignKey(interest => interest.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(interest => interest.InvestorUser)
                .WithMany()
                .HasForeignKey(interest => interest.InvestorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NdaTemplate>(entity =>
        {
            entity.ToTable("nda_templates");
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Name).HasMaxLength(180).IsRequired();
            entity.Property(template => template.Description).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<NdaTemplateVersion>(entity =>
        {
            entity.ToTable("nda_template_versions");
            entity.HasKey(version => version.Id);
            entity.HasIndex(version => new { version.TemplateId, version.VersionNumber }).IsUnique();
            entity.Property(version => version.Content).HasColumnType("text").IsRequired();

            entity.HasOne(version => version.Template)
                .WithMany(template => template.Versions)
                .HasForeignKey(version => version.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NdaAgreement>(entity =>
        {
            entity.ToTable("nda_agreements");
            entity.HasKey(agreement => agreement.Id);
            entity.HasIndex(agreement => new { agreement.ProjectId, agreement.UserId, agreement.TemplateVersionId }).IsUnique();
            entity.Property(agreement => agreement.AgreementSnapshot).HasColumnType("text").IsRequired();
            entity.Property(agreement => agreement.IpAddress).HasMaxLength(64);
            entity.Property(agreement => agreement.UserAgent).HasMaxLength(500);

            entity.HasOne(agreement => agreement.Project)
                .WithMany()
                .HasForeignKey(agreement => agreement.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(agreement => agreement.User)
                .WithMany()
                .HasForeignKey(agreement => agreement.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(agreement => agreement.Template)
                .WithMany()
                .HasForeignKey(agreement => agreement.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(agreement => agreement.TemplateVersion)
                .WithMany()
                .HasForeignKey(agreement => agreement.TemplateVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static Role CreateRole(string id, string code, string name, string description)
    {
        return new Role
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            Description = description,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
    }

    private static Skill CreateSkill(string id, string name, string normalizedName)
    {
        return new Skill
        {
            Id = Guid.Parse(id),
            Name = name,
            NormalizedName = normalizedName,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
    }

    private static SubscriptionPlan CreatePlan(string id, string code, string name, string description, decimal monthlyPrice)
    {
        return new SubscriptionPlan
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            Description = description,
            MonthlyPrice = monthlyPrice,
            Currency = "USD",
            IsActive = true,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
    }

    private static UsageQuota CreateQuota(string id, string planId, string resourceKey, int limit)
    {
        return new UsageQuota
        {
            Id = Guid.Parse(id),
            PlanId = Guid.Parse(planId),
            ResourceKey = resourceKey,
            Limit = limit,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
    }
}

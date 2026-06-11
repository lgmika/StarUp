using Microsoft.EntityFrameworkCore;
using StartupConnect.Domain.Entities;

namespace StartupConnect.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<UserSkill> UserSkills => Set<UserSkill>();

    public DbSet<Cv> CVs => Set<Cv>();

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    public DbSet<StoredFile> Files => Set<StoredFile>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectVersion> ProjectVersions => Set<ProjectVersion>();

    public DbSet<ProjectVisibilitySetting> ProjectVisibilitySettings => Set<ProjectVisibilitySetting>();

    public DbSet<ProjectRequiredRole> ProjectRequiredRoles => Set<ProjectRequiredRole>();

    public DbSet<ProjectRequiredSkill> ProjectRequiredSkills => Set<ProjectRequiredSkill>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<ProjectAccessGrant> ProjectAccessGrants => Set<ProjectAccessGrant>();

    public DbSet<SavedProject> SavedProjects => Set<SavedProject>();

    public DbSet<AIRequest> AIRequests => Set<AIRequest>();

    public DbSet<AIReview> AIReviews => Set<AIReview>();

    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();

    public DbSet<ProjectModerationReview> ProjectModerationReviews => Set<ProjectModerationReview>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ReportAction> ReportActions => Set<ReportAction>();

    public DbSet<ProjectApplication> ProjectApplications => Set<ProjectApplication>();

    public DbSet<ApplicationAttachment> ApplicationAttachments => Set<ApplicationAttachment>();

    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();

    public DbSet<InvestorProfile> InvestorProfiles => Set<InvestorProfile>();

    public DbSet<InvestorProjectInterest> InvestorProjectInterests => Set<InvestorProjectInterest>();

    public DbSet<NdaTemplate> NdaTemplates => Set<NdaTemplate>();

    public DbSet<NdaTemplateVersion> NdaTemplateVersions => Set<NdaTemplateVersion>();

    public DbSet<NdaAgreement> NdaAgreements => Set<NdaAgreement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        modelBuilder.Entity<AIRequest>(entity =>
        {
            entity.ToTable("ai_requests");
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.UserId, request.CreatedAt });
            entity.Property(request => request.PromptSnapshot).HasMaxLength(4000).IsRequired();
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

            entity.HasOne(report => report.ReporterUser)
                .WithMany()
                .HasForeignKey(report => report.ReporterUserId)
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
}

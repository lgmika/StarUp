using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Email.Interfaces;
using StartupConnect.Application.Email.Models;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.Email;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Admin;

namespace StartupConnect.Tests;

public sealed class EmailOutboxTests
{
    [Fact]
    public void EmailOutboxProtector_Should_RoundTrip_Without_Storing_Plaintext()
    {
        var protector = CreateProtector();
        const string actionUrl = "https://app.example.com/reset?token=sensitive-token";

        var protectedPayload = protector.Protect(actionUrl);

        Assert.DoesNotContain("sensitive-token", protectedPayload);
        Assert.Equal(actionUrl, protector.Unprotect(protectedPayload));
    }

    [Fact]
    public void EmailOutboxProtector_Should_Reject_Tampered_Payload()
    {
        var protector = CreateProtector();
        var payload = Convert.FromBase64String(protector.Protect("https://app.example.com/verify"));
        payload[^1] ^= 0x01;

        Assert.Throws<InvalidOperationException>(() => protector.Unprotect(Convert.ToBase64String(payload)));
    }

    [Fact]
    public void QueueProjectInvitation_Should_Support_External_Recipients_And_Encrypt_Payload()
    {
        using var dbContext = CreateDbContext();
        var dispatcher = new EmailOutboxDispatcher(
            dbContext,
            new NoOpEmailService(),
            CreateProtector(),
            Options.Create(new EmailOutboxOptions()),
            Options.Create(new EmailOptions { AppBaseUrl = "https://app.example.com" }),
            new SystemSettingReader(dbContext),
            NullLogger<EmailOutboxDispatcher>.Instance);

        var message = dispatcher.QueueProjectInvitation(
            null,
            "external@example.com",
            new ProjectInvitationEmailModel("Secret Project", "Founder", "Developer", "/invitations/1"));

        Assert.Null(message.UserId);
        Assert.DoesNotContain("Secret Project", message.ProtectedPayload);
        Assert.Contains("https://app.example.com/invitations/1", CreateProtector().Unprotect(message.ProtectedPayload));
        Assert.Equal(DeleteBehavior.SetNull,
            dbContext.Model.FindEntityType(typeof(EmailOutboxMessage))!
                .GetForeignKeys()
                .Single(foreignKey => foreignKey.Properties.Any(property => property.Name == nameof(EmailOutboxMessage.UserId)))
                .DeleteBehavior);
    }

    private static EmailOutboxProtector CreateProtector()
    {
        return new EmailOutboxProtector(Options.Create(new EmailOutboxOptions
        {
            EncryptionKey = "test_email_outbox_encryption_key_at_least_32_chars"
        }));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=startupconnect_model_test")
            .Options;
        return new AppDbContext(options);
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendEmailVerificationAsync(string email, string verificationUrl, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendProjectInvitationAsync(string email, ProjectInvitationEmailModel model, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendNotificationEmailAsync(string email, NotificationEmailModel model, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

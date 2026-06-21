using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using StartupConnect.Application.Email.Interfaces;
using StartupConnect.Application.Email.Models;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.Persistence;

namespace StartupConnect.Infrastructure.Email;

public sealed class EmailOutboxDispatcher(
    AppDbContext dbContext,
    IEmailService emailService,
    EmailOutboxProtector protector,
    IOptions<EmailOutboxOptions> optionsAccessor,
    IOptions<EmailOptions> emailOptionsAccessor,
    ISystemSettingReader systemSettingReader,
    ILogger<EmailOutboxDispatcher> logger)
{
    public const string VerificationTemplate = "EmailVerification";
    public const string PasswordResetTemplate = "PasswordReset";
    public const string ProjectInvitationTemplate = "ProjectInvitation";
    public const string NotificationTemplate = "Notification";
    private readonly EmailOutboxOptions options = optionsAccessor.Value;
    private readonly EmailOptions emailOptions = emailOptionsAccessor.Value;

    public EmailOutboxMessage QueueVerification(Guid userId, string email, string verificationUrl)
    {
        return Queue(userId, email, VerificationTemplate, new ActionPayload(verificationUrl));
    }

    public EmailOutboxMessage QueuePasswordReset(Guid userId, string email, string resetUrl)
    {
        return Queue(userId, email, PasswordResetTemplate, new ActionPayload(resetUrl));
    }

    public EmailOutboxMessage QueueProjectInvitation(Guid? userId, string email, ProjectInvitationEmailModel model)
    {
        return Queue(userId, email, ProjectInvitationTemplate, model with
        {
            InvitationUrl = BuildAppUrl(model.InvitationUrl)
        });
    }

    public EmailOutboxMessage QueueNotification(Guid userId, string email, NotificationEmailModel model)
    {
        return Queue(userId, email, NotificationTemplate, model with
        {
            ActionUrl = string.IsNullOrWhiteSpace(model.ActionUrl) ? null : BuildAppUrl(model.ActionUrl)
        });
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        if (!options.Enabled || !await systemSettingReader.GetBooleanAsync("Email.Enabled", true, cancellationToken))
        {
            return 0;
        }

        var messages = await ClaimPendingAsync(cancellationToken);

        var processed = 0;
        foreach (var message in messages)
        {
            if (await TrySendAsync(message, cancellationToken))
            {
                processed++;
            }
        }

        return processed;
    }

    private async Task<IReadOnlyCollection<EmailOutboxMessage>> ClaimPendingAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var leaseId = Guid.NewGuid();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.EmailOutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM email_outbox_messages
                WHERE "SentAt" IS NULL
                  AND "Attempts" < {options.MaxAttempts}
                  AND "NextAttemptAt" <= {now}
                  AND ("LockedUntil" IS NULL OR "LockedUntil" <= {now})
                ORDER BY "NextAttemptAt"
                LIMIT {options.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToArrayAsync(cancellationToken);

        var lockedUntil = now.AddSeconds(options.LeaseSeconds);
        foreach (var message in messages)
        {
            message.LeaseId = leaseId;
            message.LockedUntil = lockedUntil;
            message.Attempts++;
            message.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages;
    }

    private EmailOutboxMessage Queue<TPayload>(Guid? userId, string email, string template, TPayload payload)
    {
        var message = new EmailOutboxMessage
        {
            UserId = userId,
            Recipient = email,
            Template = template,
            ProtectedPayload = protector.Protect(JsonSerializer.Serialize(payload))
        };
        dbContext.EmailOutboxMessages.Add(message);
        return message;
    }

    private async Task<bool> TrySendAsync(EmailOutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(message, cancellationToken);
            message.SentAt = DateTimeOffset.UtcNow;
            message.LastError = null;
            message.LeaseId = null;
            message.LockedUntil = null;
            message.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "Email outbox lease for message {MessageId} was lost after send.", message.Id);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            message.LastError = exception.Message.Length <= 1000 ? exception.Message : exception.Message[..1000];
            message.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(Math.Pow(2, message.Attempts - 1), 60));
            message.LeaseId = null;
            message.LockedUntil = null;
            message.UpdatedAt = DateTimeOffset.UtcNow;
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException concurrencyException)
            {
                logger.LogWarning(concurrencyException, "Email outbox lease for message {MessageId} was lost.", message.Id);
                return false;
            }

            logger.LogWarning(exception, "Email outbox message {MessageId} failed on attempt {Attempt}.", message.Id, message.Attempts);
            return false;
        }
    }

    private Task SendAsync(EmailOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = protector.Unprotect(message.ProtectedPayload);
        return message.Template switch
        {
            VerificationTemplate => emailService.SendEmailVerificationAsync(message.Recipient, Deserialize<ActionPayload>(payload).ActionUrl, cancellationToken),
            PasswordResetTemplate => emailService.SendPasswordResetAsync(message.Recipient, Deserialize<ActionPayload>(payload).ActionUrl, cancellationToken),
            ProjectInvitationTemplate => emailService.SendProjectInvitationAsync(message.Recipient, Deserialize<ProjectInvitationEmailModel>(payload), cancellationToken),
            NotificationTemplate => emailService.SendNotificationEmailAsync(message.Recipient, Deserialize<NotificationEmailModel>(payload), cancellationToken),
            _ => throw new InvalidOperationException($"Unknown email outbox template '{message.Template}'.")
        };
    }

    private static TPayload Deserialize<TPayload>(string payload)
    {
        return JsonSerializer.Deserialize<TPayload>(payload)
            ?? throw new InvalidOperationException($"Email outbox payload for '{typeof(TPayload).Name}' is invalid.");
    }

    private string BuildAppUrl(string actionUrl)
    {
        if (!Uri.TryCreate(emailOptions.AppBaseUrl, UriKind.Absolute, out var appBaseUri))
        {
            throw new InvalidOperationException("Email:AppBaseUrl must be an absolute URL.");
        }

        if (Uri.TryCreate(actionUrl, UriKind.Absolute, out var absoluteUri))
        {
            if (!string.Equals(absoluteUri.Scheme, appBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(absoluteUri.Authority, appBaseUri.Authority, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Email action URL must use the configured Email:AppBaseUrl origin.");
            }

            return absoluteUri.ToString();
        }

        return new Uri(appBaseUri, actionUrl.StartsWith('/') ? actionUrl : $"/{actionUrl}").ToString();
    }

    private sealed record ActionPayload(string ActionUrl);
}

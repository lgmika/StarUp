using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StartupConnect.Application.BackgroundJobs.Dtos;
using StartupConnect.Application.BackgroundJobs.Interfaces;
using StartupConnect.Application.Files.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;

namespace StartupConnect.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobService(
    AppDbContext dbContext,
    IFileStorageService fileStorageService,
    IOptions<BackgroundJobOptions> optionsAccessor,
    ILogger<BackgroundJobService> logger) : IBackgroundJobService
{
    private readonly BackgroundJobOptions options = optionsAccessor.Value;

    public async Task<BackgroundJobRunResult> RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        var lockAcquired = await TryAcquireLockAsync(options.MaintenanceLockKey, cancellationToken);
        if (!lockAcquired)
        {
            var skipped = await RecordExecutionAsync(
                "MaintenanceLock",
                BackgroundJobStatus.Skipped,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                1,
                0,
                "Another instance is already running maintenance jobs.",
                cancellationToken);

            return new BackgroundJobRunResult(false, [Map(skipped)]);
        }

        try
        {
            var executions = new List<BackgroundJobExecution>
            {
                await RunWithRetryAsync("CleanupExpiredTokens", CleanupExpiredTokensAsync, cancellationToken),
                await RunWithRetryAsync("ExpireProjectInvitations", ExpireProjectInvitationsAsync, cancellationToken),
                await RunWithRetryAsync("CleanupOrphanFiles", CleanupOrphanFilesAsync, cancellationToken),
                await RunWithRetryAsync("ExpireSubscriptions", ExpireSubscriptionsAsync, cancellationToken),
                await RunWithRetryAsync("GenerateAnalyticsAggregate", GenerateAnalyticsAggregateAsync, cancellationToken)
            };

            return new BackgroundJobRunResult(true, executions.Select(Map).ToList());
        }
        finally
        {
            await ReleaseLockAsync(options.MaintenanceLockKey, CancellationToken.None);
        }
    }

    public async Task<IReadOnlyCollection<BackgroundJobExecutionDto>> GetRecentExecutionsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        return await dbContext.BackgroundJobExecutions
            .AsNoTracking()
            .OrderByDescending(execution => execution.StartedAt)
            .Take(safeLimit)
            .Select(execution => Map(execution))
            .ToListAsync(cancellationToken);
    }

    private async Task<BackgroundJobExecution> RunWithRetryAsync(
        string jobName,
        Func<DateTimeOffset, CancellationToken, Task<int>> action,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, options.MaxRetryAttempts);
        BackgroundJobExecution? lastExecution = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var startedAt = DateTimeOffset.UtcNow;
            try
            {
                var itemsProcessed = await action(startedAt, cancellationToken);
                return await RecordExecutionAsync(
                    jobName,
                    BackgroundJobStatus.Succeeded,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    attempt,
                    itemsProcessed,
                    null,
                    cancellationToken);
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(exception, "Background job {JobName} failed on attempt {Attempt}.", jobName, attempt);
                lastExecution = await RecordExecutionAsync(
                    jobName,
                    BackgroundJobStatus.Failed,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    attempt,
                    0,
                    exception.Message,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Background job {JobName} moved to failed state after {Attempt} attempts.", jobName, attempt);
                return await RecordExecutionAsync(
                    jobName,
                    BackgroundJobStatus.Failed,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    attempt,
                    0,
                    exception.Message,
                    cancellationToken);
            }
        }

        return lastExecution ?? throw new InvalidOperationException($"Background job '{jobName}' did not produce an execution record.");
    }

    private async Task<int> CleanupExpiredTokensAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var staleUsedBefore = now.AddDays(-7);

        var emailTokens = await dbContext.EmailVerificationTokens
            .Where(token => token.ExpiresAt <= now || (token.UsedAt != null && token.UsedAt <= staleUsedBefore))
            .OrderBy(token => token.ExpiresAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        var resetTokens = await dbContext.PasswordResetTokens
            .Where(token => token.ExpiresAt <= now || (token.UsedAt != null && token.UsedAt <= staleUsedBefore))
            .OrderBy(token => token.ExpiresAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        var refreshTokens = await dbContext.RefreshTokens
            .Where(token => token.RevokedAt == null && token.ExpiresAt <= now)
            .OrderBy(token => token.ExpiresAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        dbContext.EmailVerificationTokens.RemoveRange(emailTokens);
        dbContext.PasswordResetTokens.RemoveRange(resetTokens);

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = now;
            token.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return emailTokens.Count + resetTokens.Count + refreshTokens.Count;
    }

    private async Task<int> ExpireProjectInvitationsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var invitations = await dbContext.ProjectInvitations
            .Where(invitation => invitation.Status == ProjectInvitationStatus.Pending && invitation.ExpiresAt <= now)
            .OrderBy(invitation => invitation.ExpiresAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var invitation in invitations)
        {
            invitation.Status = ProjectInvitationStatus.Expired;
            invitation.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return invitations.Count;
    }

    private async Task<int> CleanupOrphanFilesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cutoff = now.AddDays(-1);
        var files = await dbContext.Files
            .Where(file => file.CreatedAt <= cutoff)
            .Where(file =>
                file.IsDeleted ||
                (!dbContext.CVs.Any(cv => cv.FileId == file.Id) &&
                 !dbContext.ApplicationAttachments.Any(attachment => attachment.FileId == file.Id) &&
                 !dbContext.MessageAttachments.Any(attachment => attachment.FileId == file.Id)))
            .OrderBy(file => file.CreatedAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            await fileStorageService.DeleteAsync(file.StoragePath, cancellationToken);
            file.IsDeleted = true;
            file.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return files.Count;
    }

    private async Task<int> ExpireSubscriptionsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.UserSubscriptions
            .Where(subscription => subscription.Status != SubscriptionStatus.Expired && subscription.CurrentPeriodEnd <= now)
            .OrderBy(subscription => subscription.CurrentPeriodEnd)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var subscription in subscriptions)
        {
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return subscriptions.Count;
    }

    private Task<int> GenerateAnalyticsAggregateAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }

    private async Task<BackgroundJobExecution> RecordExecutionAsync(
        string jobName,
        BackgroundJobStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        int attempt,
        int itemsProcessed,
        string? error,
        CancellationToken cancellationToken)
    {
        var execution = new BackgroundJobExecution
        {
            JobName = jobName,
            Status = status,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Attempt = attempt,
            ItemsProcessed = itemsProcessed,
            LockKey = options.MaintenanceLockKey,
            Error = error is null ? null : error[..Math.Min(error.Length, 1000)]
        };

        dbContext.BackgroundJobExecutions.Add(execution);
        await dbContext.SaveChangesAsync(cancellationToken);
        return execution;
    }

    private async Task<bool> TryAcquireLockAsync(long lockKey, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_try_advisory_lock(@lock_key)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.Value = lockKey;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool acquired && acquired;
    }

    private async Task ReleaseLockAsync(long lockKey, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock(@lock_key)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.Value = lockKey;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static BackgroundJobExecutionDto Map(BackgroundJobExecution execution)
    {
        return new BackgroundJobExecutionDto(
            execution.Id,
            execution.JobName,
            execution.Status,
            execution.StartedAt,
            execution.FinishedAt,
            execution.Attempt,
            execution.ItemsProcessed,
            execution.LockKey,
            execution.Error);
    }
}

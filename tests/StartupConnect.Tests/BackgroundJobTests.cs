using StartupConnect.Application.BackgroundJobs.Dtos;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.BackgroundJobs;

namespace StartupConnect.Tests;

public sealed class BackgroundJobTests
{
    [Fact]
    public void BackgroundJobOptions_Should_Have_Production_Safe_Defaults()
    {
        var options = new BackgroundJobOptions();

        Assert.True(options.Enabled);
        Assert.Equal(15, options.IntervalMinutes);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.True(options.BatchSize > 0);
        Assert.Equal(30, options.EmailOutboxRetentionDays);
        Assert.True(options.FailedEmailOutboxRetentionDays >= options.EmailOutboxRetentionDays);
        Assert.Equal(90, options.ExecutionRetentionDays);
        Assert.Equal(30, options.RefreshTokenRetentionDays);
        Assert.NotEqual(0, options.MaintenanceLockKey);
    }

    [Fact]
    public void BackgroundJobExecutionDto_Should_Expose_Failed_State()
    {
        var dto = new BackgroundJobExecutionDto(
            Guid.NewGuid(),
            "CleanupExpiredTokens",
            BackgroundJobStatus.Failed,
            DateTimeOffset.UtcNow.AddSeconds(-2),
            DateTimeOffset.UtcNow,
            3,
            0,
            25061225,
            "timeout");

        Assert.Equal(BackgroundJobStatus.Failed, dto.Status);
        Assert.Equal(3, dto.Attempt);
        Assert.Equal("timeout", dto.Error);
    }
}

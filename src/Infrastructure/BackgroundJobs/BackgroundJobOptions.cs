namespace StartupConnect.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 15;

    public int MaxRetryAttempts { get; set; } = 3;

    public int BatchSize { get; set; } = 100;

    public long MaintenanceLockKey { get; set; } = 25061225;
}

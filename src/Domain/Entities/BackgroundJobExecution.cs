using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class BackgroundJobExecution : BaseEntity
{
    public string JobName { get; set; } = string.Empty;

    public BackgroundJobStatus Status { get; set; } = BackgroundJobStatus.Succeeded;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset FinishedAt { get; set; } = DateTimeOffset.UtcNow;

    public int Attempt { get; set; }

    public int ItemsProcessed { get; set; }

    public long? LockKey { get; set; }

    public string? Error { get; set; }
}

using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.BackgroundJobs.Dtos;

public sealed record BackgroundJobExecutionDto(
    Guid Id,
    string JobName,
    BackgroundJobStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    int Attempt,
    int ItemsProcessed,
    long? LockKey,
    string? Error);

public sealed record BackgroundJobRunResult(
    bool LockAcquired,
    IReadOnlyCollection<BackgroundJobExecutionDto> Executions);

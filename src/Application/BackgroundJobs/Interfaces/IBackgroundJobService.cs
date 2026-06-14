using StartupConnect.Application.BackgroundJobs.Dtos;

namespace StartupConnect.Application.BackgroundJobs.Interfaces;

public interface IBackgroundJobService
{
    Task<BackgroundJobRunResult> RunMaintenanceAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BackgroundJobExecutionDto>> GetRecentExecutionsAsync(int limit, CancellationToken cancellationToken);
}

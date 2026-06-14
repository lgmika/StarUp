using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StartupConnect.Application.BackgroundJobs.Interfaces;

namespace StartupConnect.Infrastructure.BackgroundJobs;

public sealed class StartupConnectBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobOptions> optionsAccessor,
    ILogger<StartupConnectBackgroundWorker> logger) : BackgroundService
{
    private readonly BackgroundJobOptions options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("StartupConnect background jobs are disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.IntervalMinutes)));
        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
            await backgroundJobService.RunMaintenanceAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "StartupConnect background job worker failed.");
        }
    }
}

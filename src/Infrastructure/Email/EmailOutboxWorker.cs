using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StartupConnect.Infrastructure.Email;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOutboxOptions> optionsAccessor,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    private readonly EmailOutboxOptions options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.PollSeconds));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<EmailOutboxDispatcher>();
                await dispatcher.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Email outbox polling failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

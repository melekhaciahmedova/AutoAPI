using AutoAPI.Core.Services;

namespace AutoAPI.Builder.BackgroundServices;

public class MigrationWorkerService(
    ILogger<MigrationWorkerService> logger,
    MigrationWatcherService watcher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Builder migration watcher running...");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);
        }
    }

    public async Task RunMigrationAsync()
    {
        await watcher.TriggerManualMigrationAsync();
    }
}
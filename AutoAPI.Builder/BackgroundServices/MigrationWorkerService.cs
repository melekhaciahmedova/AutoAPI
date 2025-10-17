using AutoAPI.Core.Services;

namespace AutoAPI.Builder.BackgroundServices;

public class MigrationWorkerService(
    ILogger<MigrationWorkerService> logger,
    MigrationWatcherService watcher) : BackgroundService
{
    // Worker servisi boş çalışmaya devam eder (dosya izleme için)
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Builder migration watcher running...");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);
        }
    }

    // ✅ GÜNCELLEME: RunMigration metodunu async Task döndürecek şekilde değiştirin.
    // Artık Program.cs bu metodu await edecek.
    public async Task RunMigrationAsync()
    {
        // NOT: MigrationWatcherService'teki metodun adını TriggerManualMigrationAsync olarak değiştirin.
        await watcher.TriggerManualMigrationAsync();
    }
}
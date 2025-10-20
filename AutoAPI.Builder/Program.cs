using AutoAPI.Builder.BackgroundServices;
using AutoAPI.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<MigrationWatcherService>();
builder.Services.AddSingleton<MigrationWorkerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MigrationWorkerService>());

var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    if (args.Contains("migrate", StringComparer.OrdinalIgnoreCase))
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var worker = app.Services.GetRequiredService<MigrationWorkerService>();
        logger.LogInformation("🧩 Migration parametresi algılandı, RunMigrationAsync() başlatılıyor...");

        Task.Run(async () =>
        {
            try
            {
                await worker.RunMigrationAsync();
                logger.LogInformation("✅ Migration işlemi başarıyla tamamlandı.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Migration işlemi sırasında hata oluştu.");
            }
            finally
            {
                app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            }
        });
    }
});

await app.RunAsync();
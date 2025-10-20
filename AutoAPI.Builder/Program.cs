using AutoAPI.Builder.BackgroundServices;
using AutoAPI.Core.Generation;
using AutoAPI.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

// Hizmetleri kaydet
builder.Services.AddSingleton<TemplateRenderer>();
builder.Services.AddSingleton<MigrationWatcherService>();
builder.Services.AddSingleton<MigrationWorkerService>();

// Arka plan worker servisi
builder.Services.AddHostedService(provider => provider.GetRequiredService<MigrationWorkerService>());

var app = builder.Build();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

// ✅ Migration işlemleri
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
                logger.LogInformation("✅ Migration işlemi tamamlandı.");
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

    // ✅ Context generation işlemi
    if (args.Contains("generate-context", StringComparer.OrdinalIgnoreCase))
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var renderer = new TemplateRenderer();

        Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("🧩 AppDbContext generation started...");

                var generator = new AppDbContextGeneratorService(renderer, "/src");
                await generator.GenerateAppDbContextAsync([]);

                logger.LogInformation("✅ AppDbContext generation completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ AppDbContext generation failed.");
            }
            finally
            {
                app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            }
        });
    }
});

await app.RunAsync();
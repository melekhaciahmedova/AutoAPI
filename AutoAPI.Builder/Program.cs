using AutoAPI.Builder.BackgroundServices;
using AutoAPI.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<MigrationWatcherService>();
builder.Services.AddSingleton<MigrationWorkerService>();
// Arka plan servisi olarak Worker'ı kaydedin.
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

        // ✅ KESİN ÇÖZÜM: Yeni bir Task başlatarak migration'ın bitmesini bekleyin.
        // Bu, EF komutunun tüm çıktıyı göndermesine izin verir.
        Task.Run(async () =>
        {
            try
            {
                // Migration işlemi bitene kadar BEKLE
                await worker.RunMigrationAsync();
                logger.LogInformation("✅ Migration işlemi başarıyla tamamlandı.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Migration işlemi sırasında hata oluştu.");
            }
            finally
            {
                // İşlem bittikten sonra Host'u KESİNLİKLE durdur
                app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            }
        });
    }
});

await app.RunAsync();
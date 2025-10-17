using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AutoAPI.Builder;

public class ManualMigrationTrigger(ILogger<ManualMigrationTrigger> logger, MigrationWatcherService watcher) : IHostedService
{
    private readonly ILogger<ManualMigrationTrigger> _logger = logger;
    private readonly MigrationWatcherService _watcher = watcher;
    private IHost? _httpHost;
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ManualMigrationTrigger HTTP listener...");

        // Minimal API tabanlı WebApplication oluşturuyoruz
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://0.0.0.0:5230");

        // Servisler — mevcut watcher'ı yeniden inject edebilmek için singleton olarak ekliyoruz
        builder.Services.AddSingleton(_watcher);

        var app = builder.Build();

        // HTTP endpoint tanımı
        app.MapPost("/trigger-migration", async (MigrationWatcherService watcher) =>
        {
            await watcher.TriggerManualMigrationAsync();
            return Results.Ok("Migration started successfully!");
        });

        _logger.LogInformation("HTTP listener active at http://0.0.0.0:5230");

        // HTTP hostu ayrı task olarak başlatıyoruz
        _httpHost = app;
        _ = Task.Run(() => app.RunAsync(cancellationToken), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_httpHost is not null)
        {
            _logger.LogInformation("Stopping ManualMigrationTrigger HTTP listener...");
            await _httpHost.StopAsync(cancellationToken);
        }
    }
}
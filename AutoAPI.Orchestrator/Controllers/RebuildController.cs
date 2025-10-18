using Microsoft.AspNetCore.Mvc;
using AutoAPI.Core.Services;

namespace AutoAPI.Orchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RebuildController(DockerService docker, ILogger<RebuildController> logger) : ControllerBase
{
    private readonly DockerService _docker = docker;
    private readonly ILogger<RebuildController> _logger = logger;

    [HttpPost("api")]
    public async Task<IActionResult> RebuildApi()
    {
        _logger.LogInformation("♻️ AutoAPI rebuild işlemi başlatıldı...");

        var steps = new List<object>();

        const string builderContainer = "autoapi-builder";
        const string apiContainer = "autoapi-api";

        // 1️⃣ Builder konteynerinde publish işlemini yap
        var buildCmd =
            $"docker exec {builderContainer} bash -c 'cd /src/AutoAPI.API && dotnet restore && dotnet publish -c Release -o /src/AutoAPI.API/bin/Release/net8.0/publish'";
        var build = await _docker.RunCommandAsync(buildCmd);
        steps.Add(new { step = "dotnet publish", build.exitCode, build.output, build.error });

        if (build.exitCode != 0)
        {
            _logger.LogError("❌ Build hatası oluştu: {Error}", build.error);
            return StatusCode(500, new
            {
                message = "❌ Build failed.",
                steps
            });
        }

        // 2️⃣ API konteynerini yeniden başlat
        var restartCmd = $"docker restart {apiContainer}";
        var restart = await _docker.RunCommandAsync(restartCmd);
        steps.Add(new { step = "restart", restart.exitCode, restart.output, restart.error });

        if (restart.exitCode != 0)
        {
            _logger.LogError("❌ API yeniden başlatılamadı: {Error}", restart.error);
            return StatusCode(500, new
            {
                message = "❌ API restart failed.",
                steps
            });
        }

        _logger.LogInformation("✅ AutoAPI rebuild başarıyla tamamlandı!");
        return Ok(new
        {
            message = "✅ API rebuild completed successfully.",
            steps
        });
    }
}
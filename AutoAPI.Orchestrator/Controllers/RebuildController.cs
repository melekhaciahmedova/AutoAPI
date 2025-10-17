using Microsoft.AspNetCore.Mvc;
using AutoAPI.Core.Services;

namespace AutoAPI.Orchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RebuildController : ControllerBase
{
    private readonly DockerService _docker;
    private readonly ILogger<RebuildController> _logger;

    public RebuildController(DockerService docker, ILogger<RebuildController> logger)
    {
        _docker = docker;
        _logger = logger;
    }

    [HttpPost("api")]
    public async Task<IActionResult> RebuildApi()
    {
        _logger.LogInformation("♻️ AutoAPI rebuild işlemi başlatıldı...");

        string composePath = "/src";
        string serviceName = "autoapi-api";
        var steps = new List<object>();

        // 1️⃣ Container'ı zorla sil
        var rmCmd = $"docker rm -f {serviceName}";
        var rm = await _docker.RunCommandAsync(rmCmd);
        steps.Add(new { step = "remove", rm.exitCode, rm.output, rm.error });

        // 2️⃣ Servisi yeniden build et
        var buildCmd = $"docker compose -f \"{composePath}/docker-compose.yml\" build {serviceName}";
        var build = await _docker.RunCommandAsync(buildCmd);
        steps.Add(new { step = "build", build.exitCode, build.output, build.error });

        // 3️⃣ Servisi yeniden ayağa kaldır
        var upCmd = $"docker compose -f \"{composePath}/docker-compose.yml\" up -d --no-deps {serviceName}";
        var up = await _docker.RunCommandAsync(upCmd);
        steps.Add(new { step = "up", up.exitCode, up.output, up.error });

        // ❌ Hata kontrolü
        if (rm.exitCode != 0 || build.exitCode != 0 || up.exitCode != 0)
        {
            _logger.LogError("❌ API rebuild sırasında hata oluştu.");
            return StatusCode(500, new
            {
                message = "❌ API rebuild failed.",
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
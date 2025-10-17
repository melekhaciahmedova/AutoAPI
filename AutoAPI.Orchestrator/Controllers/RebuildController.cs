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
        _logger.LogInformation("♻️ AutoAPI rebuild işlemi (Compose tabanlı) başlatıldı...");

        string composePath = "/src";
        string serviceName = "autoapi-api";
        var steps = new List<(string step, int exitCode, string output, string error)>();

        // 1️⃣ Down only the API container
        var downCmd = $"docker compose -f \"{composePath}/docker-compose.yml\" down {serviceName}";
        var down = await _docker.RunCommandAsync(downCmd);
        steps.Add(("down", down.exitCode, down.output, down.error));

        // 2️⃣ Build the API container
        var buildCmd = $"docker compose -f \"{composePath}/docker-compose.yml\" build {serviceName}";
        var build = await _docker.RunCommandAsync(buildCmd);
        steps.Add(("build", build.exitCode, build.output, build.error));

        // 3️⃣ Bring the API container up (without affecting others)
        var upCmd = $"docker compose -f \"{composePath}/docker-compose.yml\" up -d --no-deps {serviceName}";
        var up = await _docker.RunCommandAsync(upCmd);
        steps.Add(("up", up.exitCode, up.output, up.error));

        // ❌ Error Handling
        if (steps.Any(s => s.exitCode != 0))
        {
            _logger.LogError("❌ Rebuild sırasında hata oluştu.");
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
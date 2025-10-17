using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RebuildController(DockerService docker, ILogger<RebuildController> logger) : ControllerBase
{
    private readonly DockerService _docker = docker;
    private readonly ILogger<RebuildController> _logger = logger;

    [HttpPost("api")]
    public async Task<IActionResult> RebuildApi()
    {
        _logger.LogInformation("♻️ API rebuild request received...");

        var composePath = "/src"; // container içindeki docker-compose.yml yolu
        var serviceName = "autoapi-api";

        var steps = new List<DockerStepResult>();

        try
        {
            // 1️⃣ Build (image yeniden oluştur)
            var build = await _docker.RunCommandAsync(
                $"docker compose -f \"{composePath}/docker-compose.yml\" build {serviceName}");
            steps.Add(new DockerStepResult(build.exitCode, build.output, build.error));

            // 2️⃣ Up (container'ı yeniden başlat)
            var up = await _docker.RunCommandAsync(
                $"docker compose -f \"{composePath}/docker-compose.yml\" up -d {serviceName}");
            steps.Add(new DockerStepResult(up.exitCode, up.output, up.error));

            // 🔍 Hata kontrolü
            var anyError = steps.Any(s => s.ExitCode != 0);
            if (anyError)
            {
                _logger.LogError("❌ API rebuild sırasında hata oluştu.");
                return StatusCode(500, new { message = "API rebuild failed.", steps });
            }

            _logger.LogInformation("✅ API rebuild completed successfully.");
            return Ok(new { message = "API rebuild completed successfully.", steps });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ API rebuild işleminde beklenmeyen hata.");
            return StatusCode(500, new { message = "Unexpected error during rebuild.", error = ex.Message, steps });
        }
    }
}

/// <summary>
/// Her adımın sonuç çıktısı için model
/// </summary>
public record DockerStepResult(int ExitCode, string Output, string Error);
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

        var composePath = "/src";
        var steps = new List<DockerStepResult>();

        try
        {
            // 1️⃣ Yeni imaj oluştur
            var build = await _docker.RunCommandAsync(
                $"docker build -t autoapi-api -f {composePath}/AutoAPI.API/Dockerfile {composePath}");
            steps.Add(new DockerStepResult(build.exitCode, build.output, build.error));

            // 2️⃣ Eski konteyneri sil
            var rm = await _docker.RunCommandAsync("docker rm -f autoapi-api");
            steps.Add(new DockerStepResult(rm.exitCode, rm.output, rm.error));

            // 3️⃣ Yeni konteyneri ayağa kaldır
            var run = await _docker.RunCommandAsync(
                "docker run -d --name autoapi-api --network autoapi-net -p 5222:8080 autoapi-api");
            steps.Add(new DockerStepResult(run.exitCode, run.output, run.error));

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

public record DockerStepResult(int ExitCode, string Output, string Error);
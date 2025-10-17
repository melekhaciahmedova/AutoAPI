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

        string composePath = "/src";
        string serviceName = "autoapi-api";
        string imageName = $"src-{serviceName}";

        var steps = new List<(string step, int code, string output, string error)>();

        // 1️⃣ Stop container (if exists)
        var stopCheck = await _docker.RunCommandAsync($"docker ps -q -f name={serviceName}");
        if (!string.IsNullOrWhiteSpace(stopCheck.output))
            steps.Add(("stop", (await _docker.RunCommandAsync($"docker stop {serviceName}")).exitCode,
                stopCheck.output, stopCheck.error));
        else
            steps.Add(("stop", 0, "Container not running", ""));

        // 2️⃣ Remove old container (if exists)
        var rmCheck = await _docker.RunCommandAsync($"docker ps -aq -f name={serviceName}");
        if (!string.IsNullOrWhiteSpace(rmCheck.output))
            steps.Add(("remove", (await _docker.RunCommandAsync($"docker rm -f {serviceName}")).exitCode,
                rmCheck.output, rmCheck.error));
        else
            steps.Add(("remove", 0, "No old container to remove", ""));

        // 3️⃣ Build image
        var buildResult = await _docker.RunCommandAsync($"docker compose -f \"{composePath}/docker-compose.yml\" build {serviceName}");
        steps.Add(("build", buildResult.exitCode, buildResult.output, buildResult.error));

        if (buildResult.exitCode != 0)
            return StatusCode(500, new { message = "❌ Build aşaması başarısız!", steps });

        // 4️⃣ Run container manually (only autoapi-api)
        var runCmd = $"docker run -d --name {serviceName} --network autoapi-net -p 5222:8080 {imageName}";
        var runResult = await _docker.RunCommandAsync(runCmd);
        steps.Add(("run", runResult.exitCode, runResult.output, runResult.error));

        if (runResult.exitCode != 0)
            return StatusCode(500, new { message = "❌ Container başlatma başarısız!", steps });

        _logger.LogInformation("✅ API rebuild başarıyla tamamlandı!");
        return Ok(new { message = "✅ API rebuild completed successfully.", steps });
    }
}
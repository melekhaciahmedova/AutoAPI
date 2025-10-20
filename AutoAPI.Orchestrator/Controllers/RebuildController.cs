using Microsoft.AspNetCore.Mvc;
using AutoAPI.Core.Services;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebuildController(DockerService docker, ILogger<RebuildController> logger)
        : ControllerBase
    {
        private readonly DockerService _docker = docker;
        private readonly ILogger<RebuildController> _logger = logger;

        [HttpPost("rebuild-lite")]
        public async Task<IActionResult> RebuildLite()
        {
            _logger.LogInformation("🔁 Rebuild-lite işlemi başlatıldı...");
            var steps = new List<object>();
            const string composeFilePath = "/src/docker-compose.yml";

            async Task<(int exitCode, string output, string error)> RunCommand(string stepName, string command)
            {
                _logger.LogInformation("▶️ [{Step}] {Command}", stepName, command);
                var result = await _docker.RunCommandAsync(command);
                steps.Add(new { step = stepName, result.exitCode, result.output, result.error });
                return result;
            }

            // 0️⃣ Context + Entities derlemesi
            var buildContext = await RunCommand("dotnet build AutoAPI.Data",
                "docker exec autoapi-builder sh -c \"cd /src/AutoAPI.Data && dotnet build -c Release\"");
            if (buildContext.exitCode != 0)
                return StatusCode(500, new { message = "❌ Context build failed.", steps });

            // 1️⃣ Down
            var down = await RunCommand("docker compose down",
                $"docker compose -f {composeFilePath} down");
            if (down.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose down failed", steps });

            // 2️⃣ Build
            var build = await RunCommand("docker compose build",
                $"docker compose -f {composeFilePath} build --no-cache");
            if (build.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose build failed", steps });

            // 3️⃣ Up
            var up = await RunCommand("docker compose up",
                $"docker compose -f {composeFilePath} up -d");
            if (up.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose up failed", steps });

            _logger.LogInformation("✅ Rebuild-lite başarıyla tamamlandı.");
            return Ok(new { message = "✅ Rebuild-lite completed successfully.", steps });
        }
    }
}
using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebuildController(DockerService docker, ILogger<RebuildController> logger)
        : ControllerBase
    {
        private readonly DockerService _docker = docker;
        private readonly ILogger<RebuildController> _logger = logger;

        public record ApiResult(bool Success, string Message, List<object> Steps, string? Error = null);

        [HttpPost("rebuild-lite")]
        public async Task<IActionResult> RebuildLite()
        {
            _logger.LogInformation("Rebuild-lite işlemi başlatıldı...");
            var steps = new List<object>();

            var composeFilePath = System.IO.File.Exists("/src/docker-compose.yml")
                ? "/src/docker-compose.yml"
                : "/app/docker-compose.yml";

            async Task RunStepAsync(string stepName, string command)
            {
                _logger.LogInformation("▶️ [{Step}] {Command}", stepName, command);
                var (exitCode, output, error) = await _docker.RunCommandAsync(command);

                steps.Add(new { step = stepName, exitCode, output, error });

                if (exitCode != 0)
                    throw new Exception($"{stepName} failed (exit code {exitCode})");
            }

            try
            {
                var commands = new (string Step, string Cmd)[]
                {
                    ("docker compose down", $"docker compose -f {composeFilePath} down"),
                    ("docker compose build --no-cache", $"docker compose -f {composeFilePath} build --no-cache"),
                    ("docker compose up -d", $"docker compose -f {composeFilePath} up -d")
                };

                foreach (var (step, cmd) in commands)
                    await RunStepAsync(step, cmd);

                _logger.LogInformation("Rebuild-lite başarıyla tamamlandı.");
                return Ok(new ApiResult(true, "Rebuild-lite completed successfully.", steps));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rebuild-lite işlemi başarısız oldu");
                return StatusCode(500, new ApiResult(false, "Rebuild-lite failed", steps, ex.Message));
            }
        }
    }
}
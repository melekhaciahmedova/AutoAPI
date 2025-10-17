using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.Orchestrator.Controllers
{
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

            var steps = new List<(int exitCode, string output, string error)>();

            // 1️⃣ Stop container (if running)
            steps.Add(await _docker.RunCommandAsync($"docker stop {serviceName} || true"));

            // 2️⃣ Remove old container
            steps.Add(await _docker.RunCommandAsync($"docker rm -f {serviceName} || true"));

            // 3️⃣ Rebuild image
            steps.Add(await _docker.RunCommandAsync($"docker compose -f \"{composePath}/docker-compose.yml\" build {serviceName}"));

            // 4️⃣ Start container manually (without recreating dependencies)
            steps.Add(await _docker.RunCommandAsync($"docker run -d --name {serviceName} --network autoapi-net -p 5222:8080 src-{serviceName}"));

            // ✅ Success check
            if (steps.Any(s => s.exitCode != 0))
            {
                _logger.LogError("❌ Rebuild sırasında hata oluştu!");
                return StatusCode(500, new
                {
                    message = "❌ API rebuild failed.",
                    steps = steps.Select((r, i) => new
                    {
                        step = i + 1,
                        r.exitCode,
                        r.output,
                        r.error
                    })
                });
            }

            _logger.LogInformation("✅ API rebuild başarıyla tamamlandı!");
            return Ok(new
            {
                message = "✅ API rebuild completed successfully.",
                steps = steps.Select((r, i) => new
                {
                    step = i + 1,
                    r.exitCode,
                    r.output,
                    r.error
                })
            });
        }
    }
}
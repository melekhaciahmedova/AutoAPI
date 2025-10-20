using Microsoft.AspNetCore.Mvc;
using AutoAPI.Core.Generation;
using AutoAPI.Core.Services;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebuildController(DockerService docker, ILogger<RebuildController> logger, IWebHostEnvironment env, ITemplateRenderer renderer)
        : ControllerBase
    {
        private readonly DockerService _docker = docker;
        private readonly ILogger<RebuildController> _logger = logger;
        private readonly IWebHostEnvironment _env = env;
        private readonly ITemplateRenderer _renderer = renderer;

        [HttpPost("rebuild-lite")]
        public async Task<IActionResult> RebuildLite()
        {
            _logger.LogInformation("🔁 Rebuild-lite işlemi başlatıldı...");
            var steps = new List<object>();
            const string composeFilePath = "/src/docker-compose.yml";

            // 0️⃣ AppDbContext yeniden oluştur
            try
            {
                _logger.LogInformation("🧩 AppDbContext yeniden oluşturuluyor...");
                var dbContextGenerator = new AppDbContextGeneratorService(_renderer, _env.ContentRootPath);
                await dbContextGenerator.GenerateAppDbContextAsync([]);
                steps.Add(new { step = "AppDbContext regenerated", status = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AppDbContext oluşturulamadı");
                return StatusCode(500, new { message = "❌ AppDbContext generation failed.", error = ex.Message, steps });
            }

            async Task<(int exitCode, string output, string error)> RunCommand(string stepName, string command)
            {
                _logger.LogInformation("▶️ [{Step}] {Command}", stepName, command);
                var result = await _docker.RunCommandAsync(command);
                steps.Add(new { step = stepName, result.exitCode, result.output, result.error });
                return result;
            }

            // 1️⃣ Down
            var down = await RunCommand("docker compose down", $"docker compose -f {composeFilePath} down");
            if (down.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose down failed", steps });

            // 2️⃣ Build
            var build = await RunCommand("docker compose build", $"docker compose -f {composeFilePath} build --no-cache");
            if (build.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose build failed", steps });

            // 3️⃣ Up
            var up = await RunCommand("docker compose up", $"docker compose -f {composeFilePath} up -d");
            if (up.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose up failed", steps });

            _logger.LogInformation("✅ Rebuild-lite başarıyla tamamlandı.");
            return Ok(new
            {
                message = "✅ Rebuild-lite completed successfully.",
                steps
            });
        }
    }
}
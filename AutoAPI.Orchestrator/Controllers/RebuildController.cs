using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.Orchestrator.Controllers
{
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

            var steps = new List<object>();
            const string builderContainer = "autoapi-builder";
            const string apiContainer = "autoapi-api";
            const string composeFilePath = "/src/docker-compose.yml";

            // 0️⃣ AppDbContext Builder içinde yeniden oluşturulsun
            try
            {
                _logger.LogInformation("🔁 AppDbContext builder konteynerinde oluşturuluyor...");
                var generateCmd = "dotnet /src/AutoAPI.Builder.dll generate-context";

                var generate = await _docker.RunCommandAsync(
                    $"docker exec {builderContainer} sh -c \"{generateCmd}\"");

                steps.Add(new { step = "Generate AppDbContext", generate.exitCode, generate.output, generate.error });

                if (generate.exitCode != 0)
                {
                    _logger.LogError("❌ AppDbContext builder konteynerinde oluşturulamadı: {Error}", generate.error);
                    return StatusCode(500, new { message = "❌ AppDbContext generation failed.", steps });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AppDbContext oluşturulamadı");
                return StatusCode(500, new { message = ex.Message, steps });
            }

            // 1️⃣ Solution publish (builder konteyner içinde)
            var publishCmd = $"docker exec {builderContainer} sh -c \"cd /src && dotnet publish AutoAPI.sln -c Release -o /src/out\"";
            var publish = await _docker.RunCommandAsync(publishCmd);
            steps.Add(new { step = "dotnet publish", publish.exitCode, publish.output, publish.error });

            if (publish.exitCode != 0)
            {
                _logger.LogError("❌ Publish hatası: {Error}", publish.error);
                return StatusCode(500, new { message = "❌ Publish failed.", steps });
            }

            // 2️⃣ Docker image rebuild
            var buildCmd = $"docker compose -f {composeFilePath} build --no-cache autoapi-api";
            var build = await _docker.RunCommandAsync(buildCmd);
            steps.Add(new { step = "docker compose build", build.exitCode, build.output, build.error });

            if (build.exitCode != 0)
            {
                _logger.LogError("❌ Docker build hatası: {Error}", build.error);
                return StatusCode(500, new { message = "❌ Docker build failed.", steps });
            }

            // 3️⃣ API konteynerini yeniden başlat
            var restartCmd = $"docker restart {apiContainer}";
            var restart = await _docker.RunCommandAsync(restartCmd);
            steps.Add(new { step = "restart", restart.exitCode, restart.output, restart.error });

            if (restart.exitCode != 0)
            {
                _logger.LogError("❌ API yeniden başlatılamadı: {Error}", restart.error);
                return StatusCode(500, new { message = "❌ API restart failed.", steps });
            }

            _logger.LogInformation("✅ AutoAPI rebuild başarıyla tamamlandı!");
            return Ok(new
            {
                message = "✅ API rebuild completed successfully.",
                steps
            });
        }
    }
}
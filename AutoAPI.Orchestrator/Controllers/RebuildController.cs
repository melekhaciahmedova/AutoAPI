using AutoAPI.API.Services;
using AutoAPI.API.Services.Generation;
using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebuildController(DockerService docker, ILogger<RebuildController> logger, IWebHostEnvironment env, ITemplateRenderer renderer) : ControllerBase
    {
        private readonly DockerService _docker = docker;
        private readonly ILogger<RebuildController> _logger = logger;
        private readonly IWebHostEnvironment _env = env;
        private readonly ITemplateRenderer _renderer = renderer;

        [HttpPost("api")]
        public async Task<IActionResult> RebuildApi()
        {
            _logger.LogInformation("♻️ AutoAPI rebuild işlemi başlatıldı...");

            var steps = new List<object>();
            const string builderContainer = "autoapi-builder";
            const string apiContainer = "autoapi-api";
            const string composeFilePath = "/src/docker-compose.yml";

            // 0️⃣ Context’i yeniden oluştur
            try
            {
                _logger.LogInformation("🔁 AppDbContext yeniden oluşturuluyor...");

                var dbContextGenerator = new AppDbContextGeneratorService(_renderer, _env.ContentRootPath);
                await dbContextGenerator.GenerateAppDbContextAsync([]);

                steps.Add(new { step = "AppDbContext regenerated", status = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AppDbContext yeniden oluşturulamadı");
                return StatusCode(500, new { message = "❌ AppDbContext regeneration failed.", error = ex.Message, steps });
            }

            // 1️⃣ Solution publish (builder konteyner içinde)
            var publishCmd =
                $"docker exec {builderContainer} sh -c \"cd /src && dotnet publish AutoAPI.sln -c Release -o /src/out\"";

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
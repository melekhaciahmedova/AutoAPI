// AutoAPI.Orchestrator/Controllers/OrchestratorController.cs
using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AutoAPI.Orchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestratorController : ControllerBase
{
    private readonly DockerService _docker;
    private readonly ILogger<OrchestratorController> _logger;

    public OrchestratorController(DockerService docker, ILogger<OrchestratorController> logger)
    {
        _docker = docker;
        _logger = logger;
    }

    /// <summary>
    /// End-to-end: down -> build --no-cache (builder+api) -> up builder -> ef database update -> up api -> healthcheck
    /// Opsiyonel: ?createNewMigration=true&name=AutoGen_20251017
    /// </summary>
    [HttpPost("rebuild-and-migrate")]
    public async Task<IActionResult> RebuildAndMigrateAsync([FromQuery] bool createNewMigration = false, [FromQuery] string? name = null)
    {
        var swTotal = Stopwatch.StartNew();
        var steps = new List<object>();

        const string composePath = "/src";
        const string apiName = "autoapi-api";
        const string builderName = "autoapi-builder";
        const string networkName = "autoapi_autoapi-net"; // docker compose "name: autoapi" ise bu olur; farklıysa düzelt
        const string apiUrlInNet = "http://autoapi-api:8080/swagger/index.html";

        async Task<(int code, string output, string error)> Run(string step, string cmd)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("▶️ [{Step}] {Cmd}", step, cmd);

            var (exit, stdout, stderr) = await _docker.RunCommandAsync(cmd);
            sw.Stop();

            _logger.LogInformation("✅ [{Step}] ExitCode={Exit} ({Ms} ms)", step, exit, sw.ElapsedMilliseconds);
            if (!string.IsNullOrWhiteSpace(stderr))
                _logger.LogWarning("⚠️ [{Step}] Error:\n{Err}", step, stderr);

            steps.Add(new { step, exitCode = exit, ms = sw.ElapsedMilliseconds, output = stdout, error = stderr });
            return (exit, stdout, stderr);
        }

        try
        {
            // 0) Ön temizlik: varsa containerları sil (hata sayma; yoksa yok)
            var (_, apiExistsOut, _) = await _docker.RunCommandAsync($"docker ps -aq -f name={apiName}");
            if (!string.IsNullOrWhiteSpace(apiExistsOut)) await Run("remove-api", $"docker rm -f {apiName}");

            var (_, builderExistsOut, _) = await _docker.RunCommandAsync($"docker ps -aq -f name={builderName}");
            if (!string.IsNullOrWhiteSpace(builderExistsOut)) await Run("remove-builder", $"docker rm -f {builderName}");

            // 1) Build (cache KAPALI) - sadece GEREKLİ servisler
            var (bCode, _, _) = await Run("build", $"docker compose -f \"{composePath}/docker-compose.yml\" build --no-cache {builderName} {apiName}");
            if (bCode != 0) return StatusCode(500, new { message = "Build failed", steps });

            // 2) Up (builder) - migration çalıştıracağımız için önce builder
            var (upBuilder, _, _) = await Run("up-builder", $"docker compose -f \"{composePath}/docker-compose.yml\" up -d --no-deps {builderName}");
            if (upBuilder != 0) return StatusCode(500, new { message = "Up builder failed", steps });

            // 3) EF migrations: create (opsiyonel) + update
            if (createNewMigration)
            {
                var migName = string.IsNullOrWhiteSpace(name) ? $"AutoGen_{DateTime.UtcNow:yyyyMMddHHmmss}" : name.Trim();
                // -p: Migrations project (Data), -s: Startup/host (Builder)
                var (mAdd, _, _) = await Run("ef-migrations-add",
                    $"docker exec {builderName} dotnet ef migrations add {migName} -p /src/AutoAPI.Data/AutoAPI.Data.csproj -s /src/AutoAPI.Builder/AutoAPI.Builder.csproj");
                if (mAdd != 0) return StatusCode(500, new { message = "EF migrations add failed", steps });
            }

            var (mUpdate, _, _) = await Run("ef-database-update",
                $"docker exec {builderName} dotnet ef database update -p /src/AutoAPI.Data/AutoAPI.Data.csproj -s /src/AutoAPI.Builder/AutoAPI.Builder.csproj");
            if (mUpdate != 0) return StatusCode(500, new { message = "EF database update failed", steps });

            // 4) Up (api) - yeni DLL’lerle ayağa kalksın
            var (upApi, _, _) = await Run("up-api", $"docker compose -f \"{composePath}/docker-compose.yml\" up -d --no-deps {apiName}");
            if (upApi != 0) return StatusCode(500, new { message = "Up api failed", steps });

            // 5) Healthcheck (opsiyonel) - API ayağa kalktı mı?
            // küçük bir retry ile dene
            var healthy = false;
            for (int i = 0; i < 6; i++)
            {
                var (curlCode, _, _) = await Run($"healthcheck-{i + 1}", $"docker exec {builderName} curl -s -o /dev/null -w \"%{{http_code}}\" {apiUrlInNet}");
                // curl exit 0 olsa da http_code 200 değilse output'u ayrı almak gerekir; sadeleştirmek adına sadece exitCode'a bakıyoruz.
                if (curlCode == 0) { healthy = true; break; }
                await Task.Delay(2000);
            }

            swTotal.Stop();
            _logger.LogInformation("🎉 Rebuild & Migrate tamamlandı. Total={Ms} ms, Healthy={Healthy}", swTotal.ElapsedMilliseconds, healthy);

            return Ok(new
            {
                message = "✅ Rebuild & Migration completed",
                healthy,
                totalMs = swTotal.ElapsedMilliseconds,
                steps
            });
        }
        catch (Exception ex)
        {
            swTotal.Stop();
            _logger.LogError(ex, "❌ Rebuild & Migration exception");
            steps.Add(new { step = "exception", exitCode = -1, ms = 0, output = "", error = ex.ToString() });
            return StatusCode(500, new { message = "❌ Rebuild & Migration failed", totalMs = swTotal.ElapsedMilliseconds, steps });
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok("OK");
}
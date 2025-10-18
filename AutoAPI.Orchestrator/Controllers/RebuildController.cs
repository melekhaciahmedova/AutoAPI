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

        var steps = new List<object>();

        const string builderContainer = "autoapi-builder";
        const string apiContainer = "autoapi-api";

        // 1️⃣ Builder konteynerinde publish işlemini yap
        var buildCmd =
    $"docker exec {builderContainer} sh -c \"cd /src && dotnet publish AutoAPI.sln -c Release -o /src/out\"";

        var build = await _docker.RunCommandAsync(buildCmd);
        steps.Add(new { step = "dotnet publish", build.exitCode, build.output, build.error });

        if (build.exitCode != 0)
        {
            _logger.LogError("❌ Build hatası oluştu: {Error}", build.error);
            return StatusCode(500, new
            {
                message = "❌ Build failed.",
                steps
            });
        }

        // 2️⃣ API konteynerini yeniden başlat
        var restartCmd = $"docker restart {apiContainer}";
        var restart = await _docker.RunCommandAsync(restartCmd);
        steps.Add(new { step = "restart", restart.exitCode, restart.output, restart.error });

        if (restart.exitCode != 0)
        {
            _logger.LogError("❌ API yeniden başlatılamadı: {Error}", restart.error);
            return StatusCode(500, new
            {
                message = "❌ API restart failed.",
                steps
            });
        }

        _logger.LogInformation("✅ AutoAPI rebuild başarıyla tamamlandı!");
        return Ok(new
        {
            message = "✅ API rebuild completed successfully.",
            steps
        });
    }

    [HttpPost("migrate")]
    public async Task<IActionResult> RunMigrations()
    {
        _logger.LogInformation("⚙️ Migration işlemi başlatıldı...");

        var steps = new List<object>();
        string builderContainer = "autoapi-builder";

        try
        {
            // 1️⃣ Migration adı (timestamp ile benzersiz)
            string migrationName = $"AutoMigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            string projectPath = "/src/AutoAPI.Data"; // 📦 EF Core DbContext bu projede

            // 2️⃣ Migration oluştur
            var addMigrationCmd =
                $"docker exec {builderContainer} sh -lc \"cd {projectPath} && dotnet ef migrations add {migrationName} --startup-project ../AutoAPI.API/AutoAPI.API.csproj\"";

            var addResult = await _docker.RunCommandAsync(addMigrationCmd);
            steps.Add(new { step = "add-migration", addResult.exitCode, addResult.output, addResult.error });

            if (addResult.exitCode != 0)
                throw new Exception($"Migration oluşturulamadı: {addResult.error}");

            // 3️⃣ Database update
            var updateCmd =
                $"docker exec {builderContainer} sh -lc \"cd {projectPath} && dotnet ef database update --startup-project ../AutoAPI.API/AutoAPI.API.csproj\"";

            var updateResult = await _docker.RunCommandAsync(updateCmd);
            steps.Add(new { step = "update-database", updateResult.exitCode, updateResult.output, updateResult.error });

            if (updateResult.exitCode != 0)
                throw new Exception($"Database update başarısız: {updateResult.error}");

            _logger.LogInformation("✅ Migration işlemi başarıyla tamamlandı!");
            return Ok(new
            {
                message = "✅ Migration ve Database update işlemi tamamlandı.",
                migrationName,
                steps
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Migration sırasında hata oluştu.");
            return StatusCode(500, new
            {
                message = "❌ Migration işlemi başarısız.",
                error = ex.Message,
                steps
            });
        }
    }
}
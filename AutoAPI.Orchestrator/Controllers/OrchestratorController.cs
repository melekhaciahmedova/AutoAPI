using Microsoft.AspNetCore.Mvc;
using AutoAPI.Core.Services;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrchestratorController(DockerService docker, ILogger<OrchestratorController> logger) : ControllerBase
    {
        private readonly DockerService _docker = docker;
        private readonly ILogger<OrchestratorController> _logger = logger;

        [HttpPost("migrate")]
        public async Task<IActionResult> RunMigrationAsync([FromQuery] string name = "ManualMigration")
        {
            var steps = new List<object>();

            try
            {
                var efTool = await _docker.RunCommandAsync(
                    "docker exec autoapi-builder bash -c 'mkdir -p /src/tools && " +
                    "if [ ! -f /src/tools/dotnet-ef ]; then dotnet tool install --tool-path /src/tools dotnet-ef --version 8.*; fi'"
                );
                steps.Add(new { step = "ensure-ef-tool", efTool.exitCode, efTool.output, efTool.error });
                if (efTool.exitCode != 0)
                    return StatusCode(500, new { message = "❌ EF tool installation failed.", steps });

                var migrationName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var add = await _docker.RunEfCommandAsync("ef-migrations-add",
                    $"migrations add {migrationName} " +
                    "--project /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                    "--startup-project /src/AutoAPI.API/AutoAPI.API.csproj " +
                    "--output-dir Migrations");
                steps.Add(new { step = "ef-migrations-add", add.exitCode, add.output, add.error });
                if (add.exitCode != 0)
                    return StatusCode(500, new { message = "❌ Migration add failed.", steps });

                var build = await _docker.RunCommandAsync(
                    "docker exec autoapi-builder dotnet build /src/AutoAPI.API/AutoAPI.API.csproj -c Release"
                );
                steps.Add(new { step = "api-build", build.exitCode, build.output, build.error });
                if (build.exitCode != 0)
                    return StatusCode(500, new { message = "❌ API project build failed.", steps });

                var update = await _docker.RunEfCommandAsync("ef-database-update",
                    "database update " +
                    "--project /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                    "--startup-project /src/AutoAPI.API/AutoAPI.API.csproj " +
                    "--context AppDbContext");
                steps.Add(new { step = "ef-database-update", update.exitCode, update.output, update.error });
                if (update.exitCode != 0)
                    return StatusCode(500, new { message = "❌ Database update failed.", steps });

                return Ok(new
                {
                    message = "✅ Migration added and database updated successfully.",
                    migrationName,
                    migrationPath = "/src/AutoAPI.Data/Migrations",
                    steps
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 Migration pipeline failed.");
                return StatusCode(500, new { message = ex.Message, steps });
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrchestratorController : ControllerBase
    {
        private readonly ILogger<OrchestratorController> _logger;
        private readonly string _composeFile = "/src/docker-compose.yml";
        private const string EF_TOOL_PATH = "/src/tools/dotnet-ef"; // local binary yolu

        public OrchestratorController(ILogger<OrchestratorController> logger)
        {
            _logger = logger;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerMigration([FromQuery] string name = "AutoMigration")
        {
            var steps = new List<object>();

            async Task<(int exitCode, string output, string error)> RunCommand(string stepName, string command)
            {
                _logger.LogInformation($"▶️ [{stepName}] {command}");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                int exitCode = process.ExitCode;

                if (exitCode == 0)
                    _logger.LogInformation($"✅ [{stepName}] ExitCode={exitCode}");
                else
                    _logger.LogError($"❌ [{stepName}] ExitCode={exitCode}");

                if (!string.IsNullOrWhiteSpace(error))
                    _logger.LogWarning($"⚠️ [{stepName}] Error:\n{error}");

                steps.Add(new { step = stepName, exitCode, output, error });
                return (exitCode, output, error);
            }

            // 1️⃣ Mevcut container'ları temizle
            await RunCommand("remove-api", "docker rm -f autoapi-api || true");
            await RunCommand("remove-builder", "docker rm -f autoapi-builder || true");

            // 2️⃣ Builder ve API imajlarını yeniden oluştur
            var build = await RunCommand("build",
                $"docker compose -f \"{_composeFile}\" build --no-cache autoapi-builder autoapi-api");
            if (build.exitCode != 0)
                return StatusCode(500, new { message = "❌ Build failed.", steps });

            // 3️⃣ Builder'ı ayağa kaldır
            var upBuilder = await RunCommand("up-builder",
                $"docker compose -f \"{_composeFile}\" up -d --no-deps autoapi-builder");
            if (upBuilder.exitCode != 0)
                return StatusCode(500, new { message = "❌ Builder start failed.", steps });

            // 4️⃣ EF Tool'u mevcut değilse yükle
            var ensureEfTool = await RunCommand("ensure-ef-tool",
                $"docker exec -w /src autoapi-builder bash -lc \"[ -f {EF_TOOL_PATH} ] || dotnet tool install --tool-path ./tools dotnet-ef --version 8.*\"");
            if (ensureEfTool.exitCode != 0)
                return StatusCode(500, new { message = "❌ EF tool install failed.", steps });

            // 5️⃣ Yeni migration oluştur
            var migrationName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var migrationAdd = await RunCommand("ef-migrations-add",
                $"docker exec -w /src autoapi-builder bash -lc \"{EF_TOOL_PATH} migrations add {migrationName} --project AutoAPI.Data/AutoAPI.Data.csproj --startup-project AutoAPI.API/AutoAPI.API.csproj --output-dir AutoAPI.Data/Migrations\"");
            if (migrationAdd.exitCode != 0)
                return StatusCode(500, new { message = "❌ Migration add failed.", steps });

            // 6️⃣ Database update
            var migrationUpdate = await RunCommand("ef-database-update",
                $"docker exec -w /src autoapi-builder bash -lc \"{EF_TOOL_PATH} database update --project AutoAPI.Data/AutoAPI.Data.csproj --startup-project AutoAPI.API/AutoAPI.API.csproj\"");
            if (migrationUpdate.exitCode != 0)
                return StatusCode(500, new { message = "❌ Database update failed.", steps });

            // 7️⃣ API container'ını yeniden başlat
            var upApi = await RunCommand("up-api",
                $"docker compose -f \"{_composeFile}\" up -d --no-deps autoapi-api");
            if (upApi.exitCode != 0)
                return StatusCode(500, new { message = "❌ API start failed.", steps });

            return Ok(new
            {
                message = "✅ Migration and rebuild completed successfully.",
                steps
            });
        }
    }
}
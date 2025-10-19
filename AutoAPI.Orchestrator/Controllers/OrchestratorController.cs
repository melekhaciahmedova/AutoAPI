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
        private const string EF_PATH_FIX_PREFIX = "sh -c \"PATH=/root/.dotnet/tools:$PATH ";
        private const string EF_PATH_FIX_SUFFIX = "\"";


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
                        // sh -c'yi çalıştırmak için Arguments'ı kullanıyoruz
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

                _logger.LogWarning($"⚠️ [{stepName}] Error:\n{error}");
                steps.Add(new { step = stepName, exitCode, output, error });
                return (exitCode, output, error);
            }

            // 1️⃣ Down / remove existing containers
            await RunCommand("remove-api", "docker rm -f autoapi-api || true");
            await RunCommand("remove-builder", "docker rm -f autoapi-builder || true");

            // 2️⃣ Build both projects
            var build = await RunCommand("build", $"docker compose -f \"{_composeFile}\" build --no-cache autoapi-builder autoapi-api");
            if (build.exitCode != 0)
                return StatusCode(500, new { message = "❌ Build failed.", steps });

            // 3️⃣ Start builder
            var upBuilder = await RunCommand("up-builder", $"docker compose -f \"{_composeFile}\" up -d --no-deps autoapi-builder");
            if (upBuilder.exitCode != 0)
                return StatusCode(500, new { message = "❌ Builder start failed.", steps });

            // 4️⃣ Run EF migrations add (PATH FIX UYGULANDI)
            var migrationAdd = await RunCommand("ef-migrations-add",
                $"docker exec autoapi-builder {EF_PATH_FIX_PREFIX} dotnet ef migrations add {name} " +
                $"-p /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                $"-s /src/AutoAPI.API/AutoAPI.API.csproj{EF_PATH_FIX_SUFFIX}");

            if (migrationAdd.exitCode != 0)
                return StatusCode(500, new { message = "❌ Migration add failed.", steps });

            // 5️⃣ Apply migrations to database (PATH FIX UYGULANDI)
            var migrationUpdate = await RunCommand("ef-database-update",
                $"docker exec autoapi-builder {EF_PATH_FIX_PREFIX} dotnet ef database update " +
                $"-p /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                $"-s /src/AutoAPI.API/AutoAPI.API.csproj{EF_PATH_FIX_SUFFIX}");

            if (migrationUpdate.exitCode != 0)
                return StatusCode(500, new { message = "❌ Database update failed.", steps });

            // 6️⃣ Start API container again
            var upApi = await RunCommand("up-api", $"docker compose -f \"{_composeFile}\" up -d --no-deps autoapi-api");
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
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrchestratorController(ILogger<OrchestratorController> logger) : ControllerBase
    {
        private readonly ILogger<OrchestratorController> _logger = logger;
        private const string EF_TOOL_PATH = "/src/tools/dotnet-ef";

        [HttpPost("migrate")]
        public async Task<IActionResult> RunMigrationOnly([FromQuery] string name = "ManualMigration")
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
                        // Command'i tırnak içine alarak gönderiyoruz.
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

            // 1️⃣ EF tool kontrol
            var ensureEfTool = await RunCommand("ensure-ef-tool",
                // Bu komutta zaten iç içe tırnak olduğu için böyle kalabilir.
                $"docker exec autoapi-builder bash -c 'mkdir -p /src/tools && export PATH=$PATH:/src/tools && " +
                $"if [ ! -f {EF_TOOL_PATH} ]; then dotnet tool install --tool-path /src/tools dotnet-ef --version 8.*; fi'");
            if (ensureEfTool.exitCode != 0)
                return StatusCode(500, new { message = "❌ EF tool install failed.", steps });

            // 2️⃣ Migration oluştur
            var migrationName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}";

            // DÜZELTME: Bash'e gönderilen komut içindeki tırnak işaretleri kaldırıldı.
            // Bu, bash'in komutu daha doğru bir şekilde çalıştırmasını sağlar.
            var migrationAdd = await RunCommand("ef-migrations-add",
                $"docker exec -w /src/AutoAPI.Data autoapi-builder {EF_TOOL_PATH} migrations add {migrationName} " +
                "--project /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                "--startup-project /src/AutoAPI.API/AutoAPI.API.csproj " +
                "--output-dir Migrations --force"); // Tırnak yok

            if (migrationAdd.exitCode != 0)
                return StatusCode(500, new { message = "❌ Migration add failed.", steps });

            // 3️⃣ Database update
            // DÜZELTME: Tırnak işaretleri kaldırıldı.
            var migrationUpdate = await RunCommand("ef-database-update",
                $"docker exec -w /src/AutoAPI.Data autoapi-builder {EF_TOOL_PATH} database update " +
                "--project /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                "--startup-project /src/AutoAPI.API/AutoAPI.API.csproj"); // Tırnak yok

            if (migrationUpdate.exitCode != 0)
                return StatusCode(500, new { message = "❌ Database update failed.", steps });

            return Ok(new
            {
                message = "✅ Migration added and database updated successfully.",
                migrationName,
                migrationPath = "/src/AutoAPI.Data/Migrations",
                steps
            });
        }
    }
}
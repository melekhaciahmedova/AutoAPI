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

        // 🚨 GÜNCELLENDİ: Kullanıcının sağladığı MSSQL bağlantı dizesi kullanılıyor.
        private const string DB_CONNECTION_STRING = "Server=65.108.38.170,1400;Database=auto_db;User Id=sa;Password=S!@sc0.@z;TrustServerCertificate=True";
        private const string EF_CONNECTION_STRING_ENV = "ConnectionStrings__AppDbContext"; // EF Core'un aradığı format

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
                $"docker exec autoapi-builder bash -c 'mkdir -p /src/tools && export PATH=$PATH:/src/tools && " +
                $"if [ ! -f {EF_TOOL_PATH} ]; then dotnet tool install --tool-path /src/tools dotnet-ef --version 8.*; fi'");
            if (ensureEfTool.exitCode != 0)
                return StatusCode(500, new { message = "❌ EF tool install failed.", steps });

            // 2️⃣ Migration oluştur
            var migrationName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            // Migration Add komutu, derleme ve bağlantı dizesi gereksinimlerini hafifletir.
            var migrationAdd = await RunCommand("ef-migrations-add",
                $"docker exec -w /src/AutoAPI.Data autoapi-builder {EF_TOOL_PATH} migrations add {migrationName} " +
                "--project /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                "--startup-project /src/AutoAPI.API/AutoAPI.API.csproj " +
                "--output-dir Migrations");

            if (migrationAdd.exitCode != 0)
                return StatusCode(500, new { message = "❌ Migration add failed.", steps });

            // 3️⃣ Startup projesini derle (Yeni migration'ı tanıması için)
            var buildApiProject = await RunCommand("api-project-build",
                $"docker exec autoapi-builder dotnet build /src/AutoAPI.API/AutoAPI.API.csproj");

            if (buildApiProject.exitCode != 0)
                return StatusCode(500, new { message = "❌ API project build failed.", steps });

            // 4️⃣ Database update (Bağlantı dizesi ENV olarak geçiriliyor)
            var migrationUpdate = await RunCommand("ef-database-update",
                $"docker exec -e ASPNETCORE_ENVIRONMENT=Development " +
                $"-e {EF_CONNECTION_STRING_ENV}=\"{DB_CONNECTION_STRING}\" " + // Yeni MSSQL dizesi ENV olarak ayarlandı
                $"-w /src/AutoAPI.Data autoapi-builder {EF_TOOL_PATH} database update " +
                "--project /src/AutoAPI.Data/AutoAPI.Data.csproj " +
                "--startup-project /src/AutoAPI.API/AutoAPI.API.csproj");

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

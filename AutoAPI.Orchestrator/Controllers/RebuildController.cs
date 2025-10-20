using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.Orchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RebuildController(DockerService docker, ILogger<RebuildController> logger)
        : ControllerBase
    {
        private readonly DockerService _docker = docker;
        private readonly ILogger<RebuildController> _logger = logger;

        [HttpPost("rebuild-lite")]
        public async Task<IActionResult> RebuildLite()
        {
            _logger.LogInformation("🔁 Rebuild-lite işlemi başlatıldı...");
            var steps = new List<object>();
            const string composeFilePath = "/src/docker-compose.yml";

            async Task<(int exitCode, string output, string error)> RunCommand(string stepName, string command)
            {
                _logger.LogInformation("▶️ [{Step}] {Command}", stepName, command);
                var result = await _docker.RunCommandAsync(command);
                steps.Add(new { step = stepName, result.exitCode, result.output, result.error });
                return result;
            }

            // 0️⃣ Context + Entities derlemesi
            var buildContext = await RunCommand("dotnet build AutoAPI.Data",
                "docker exec autoapi-builder sh -c \"cd /src/AutoAPI.Data && dotnet build -c Release\"");
            if (buildContext.exitCode != 0)
                return StatusCode(500, new { message = "❌ Context build failed.", steps });

            // 1️⃣ Down (stop)
            var down = await RunCommand("docker compose stop",
                $"docker compose -f {composeFilePath} stop autoapi-api autoapi-builder");
            if (down.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose stop failed", steps });

            // 2️⃣ Build
            var build = await RunCommand("docker compose build",
                $"docker compose -f {composeFilePath} build --no-cache autoapi-api autoapi-builder");
            if (build.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose build failed", steps });

            // 3️⃣ Up
            var up = await RunCommand("docker compose up",
                $"docker compose -f {composeFilePath} up -d autoapi-api autoapi-builder");
            if (up.exitCode != 0)
                return StatusCode(500, new { message = "❌ docker-compose up failed", steps });

            // 4️⃣ AppDbContext doğrulama
            _logger.LogInformation("🔍 AppDbContext doğrulaması başlatılıyor...");

            var checkCmd = """
docker exec autoapi-builder sh -c "cd /tmp && \
dotnet new console -n Checker --force >/dev/null && \
cd Checker && \
cp -r /src/AutoAPI.API/bin/Release/net8.0/* . && \
echo '#pragma warning disable
using System;
using System.Reflection;
using System.Linq;
class P {
 static void Main(){
  try {
    var asm = Assembly.LoadFrom(\"/tmp/Checker/AutoAPI.Data.dll\");
    var ctx = asm.GetTypes().Where(t => t.FullName != null && t.FullName.Contains(\"AppDbContext\")).ToList();
    if (ctx.Any())
      Console.WriteLine($\"✅ Found: {string.Join(\", \", ctx.Select(t => t.FullName))}\");
    else
      Console.WriteLine(\"❌ AppDbContext not found.\");
  } catch (Exception ex) {
    Console.WriteLine($\"❌ Exception: {ex.Message}\");
  }
 }
}' > Program.cs && \
dotnet run --no-restore"
""";


            var check = await RunCommand("AppDbContext verification", checkCmd);

            _logger.LogInformation(check.output);

            _logger.LogInformation("✅ Rebuild-lite başarıyla tamamlandı.");
            return Ok(new
            {
                message = "✅ Rebuild-lite completed successfully.",
                steps
            });
        }
    }
}
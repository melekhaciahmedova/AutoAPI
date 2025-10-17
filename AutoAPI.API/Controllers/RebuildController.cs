using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RebuildController(DockerService docker, ILogger<RebuildController> logger) : ControllerBase
{
    private readonly DockerService _docker = docker;
    private readonly ILogger<RebuildController> _logger = logger;

    [HttpPost("api")]
    public async Task<IActionResult> RebuildApi()
    {
        _logger.LogInformation("♻️ AutoAPI rebuild süreci başladı...");

        var steps = new List<DockerStepResult>();
        const string serviceName = "autoapi-api";
        const string networkName = "autoapi-net";
        const string composePath = "/src";

        try
        {
            // 1️⃣ Down (container durdur + sil)
            _logger.LogInformation("⬇️ Eski container durduruluyor...");
            var down = await _docker.RunCommandAsync($"docker rm -f {serviceName}");
            steps.Add(new DockerStepResult(down.exitCode, down.output, down.error));

            // 2️⃣ Build (yeni image oluştur)
            _logger.LogInformation("🏗️ Yeni image build ediliyor...");
            var build = await _docker.RunCommandAsync(
                $"docker build -t {serviceName} -f {composePath}/AutoAPI.API/Dockerfile {composePath}");
            steps.Add(new DockerStepResult(build.exitCode, build.output, build.error));

            if (build.exitCode != 0)
                throw new Exception("Build işlemi başarısız!");

            // 3️⃣ Up (yeni container başlat)
            _logger.LogInformation("🚀 Yeni container başlatılıyor...");
            var up = await _docker.RunCommandAsync(
                $"docker run -d --name {serviceName} --network {networkName} -p 5222:8080 {serviceName}");
            steps.Add(new DockerStepResult(up.exitCode, up.output, up.error));

            if (up.exitCode != 0)
                throw new Exception("Container başlatılamadı!");

            _logger.LogInformation("✅ AutoAPI rebuild tamamlandı.");
            return Ok(new
            {
                message = "✅ API rebuild completed successfully.",
                steps
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Rebuild işlemi hata verdi.");
            return StatusCode(500, new
            {
                message = "❌ API rebuild failed.",
                error = ex.Message,
                steps
            });
        }
    }
}

public record DockerStepResult(int ExitCode, string Output, string Error);
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AutoAPI.Core.Services;

public class DockerService
{
    private readonly ILogger<DockerService> _logger;
    private string _dockerCmd = "docker compose";
    private const string BUILDER_CONTAINER = "autoapi-builder";
    private const string EF_TOOL_PATH = "/src/tools/dotnet-ef";

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
        DetectDockerComposeCommand();
    }

    private void DetectDockerComposeCommand()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose version",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process!.WaitForExit(1500);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("'docker compose' desteklenmiyor, 'docker-compose' kullanılacak.");
                _dockerCmd = "docker-compose";
            }
        }
        catch
        {
            _logger.LogWarning("'docker compose' komutu çalıştırılamadı, 'docker-compose' kullanılacak.");
            _dockerCmd = "docker-compose";
        }
    }

    /// <summary>
    /// Docker komutlarını (veya sistem komutlarını) çalıştırmak için ortak method.
    /// </summary>
    public async Task<(int exitCode, string output, string error)> RunCommandAsync(string command)
    {
        _logger.LogInformation("▶️ Executing: {Command}", command);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

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

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            await Task.Delay(150);

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();

            if (process.ExitCode == 0)
                _logger.LogInformation("✅ Command succeeded");
            else
                _logger.LogError("❌ Command failed ({ExitCode})", process.ExitCode);

            if (!string.IsNullOrEmpty(error))
                _logger.LogWarning("⚠️ STDERR:\n{Error}", error);

            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🚨 Komut çalıştırılamadı");
            return (-1, "", ex.Message);
        }
    }

    /// <summary>
    /// Builder container içinde EF Core komutu çalıştırır (örn. migrations add, database update).
    /// </summary>
    public async Task<(int exitCode, string output, string error)> RunEfCommandAsync(string title, string efArgs)
    {
        string command =
            $"docker exec {BUILDER_CONTAINER} bash -c 'ASPNETCORE_ENVIRONMENT=Development " +
            $"{EF_TOOL_PATH} {efArgs}'";

        _logger.LogInformation("🧩 [{Step}] EF Command: {Command}", title, command);
        return await RunCommandAsync(command);
    }

    /// <summary>
    /// API servisini yeniden derler ve ayağa kaldırır.
    /// </summary>
    public async Task<bool> RebuildApiAsync(string composePath, string serviceName = "autoapi-api")
    {
        try
        {
            _logger.LogInformation("🔁 API container rebuild işlemi başlatıldı...");

            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}/docker-compose.yml\" stop {serviceName}");
            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}/docker-compose.yml\" rm -f {serviceName}");
            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}/docker-compose.yml\" build {serviceName}");
            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}/docker-compose.yml\" up -d {serviceName}");

            _logger.LogInformation("✅ API container başarıyla yeniden oluşturuldu.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API rebuild işlemi başarısız oldu.");
            return false;
        }
    }

    /// <summary>
    /// Builder container içinde migration’ı trigger eder.
    /// </summary>
    public async Task TriggerBuilderMigrationAsync()
    {
        const string cmd = "dotnet /app/AutoAPI.Builder.dll migrate";
        const string command = $"docker exec {BUILDER_CONTAINER} {cmd}";

        _logger.LogInformation("🚀 Triggering builder migration: {Command}", command);

        var (exitCode, output, error) = await RunCommandAsync(command);

        if (exitCode != 0)
        {
            _logger.LogError("❌ Builder migration trigger başarısız oldu.\n{Error}", output + error);
            throw new Exception($"Komut hatası ({exitCode}): {output + error}");
        }

        _logger.LogInformation("✅ Builder migration triggered.\n{Output}", output);
    }
}
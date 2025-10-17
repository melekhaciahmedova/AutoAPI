using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutoAPI.Core.Services;

public class DockerService
{
    private readonly ILogger<DockerService> _logger;
    private string _dockerCmd = "docker compose";

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

    public async Task<bool> RebuildApiAsync(string composePath, string serviceName = "autoapi-api")
    {
        try
        {
            _logger.LogInformation("API container rebuild işlemi başlatıldı...");

            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}\\docker-compose.yml\" stop {serviceName}");
            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}\\docker-compose.yml\" rm -f {serviceName}");
            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}\\docker-compose.yml\" build {serviceName}");
            await RunCommandAsync($"{_dockerCmd} -f \"{composePath}\\docker-compose.yml\" up -d {serviceName}");

            _logger.LogInformation("API container başarıyla yeniden oluşturuldu.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API rebuild işlemi başarısız oldu.");
            return false;
        }
    }

    /// <summary>
    /// Docker komutlarını çalıştırmak için ortak method.
    /// </summary>
    // AutoAPI.Core/Services/DockerService.cs

    public async Task<(int exitCode, string output, string error)> RunCommandAsync(string command)
    {
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        // Komutu parçala (Örn: "docker exec autoapi-builder..." -> file="docker", args="exec autoapi-builder...")
        var parts = command.Split(' ', 2);
        var file = parts[0];
        var args = parts.Length > 1 ? parts[1] : "";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        // Asenkron çıktıyı yakalama
        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Sürecin bitmesini bekleyin
            await process.WaitForExitAsync();

            // ✅ KESİN GÜNCELLEME 3: Çıktı akışlarının kapanmasını bekleyin
            // Output/Error akışları asenkron olduğu için, process kapansa bile
            // son olaylar henüz gelmemiş olabilir.
            process.WaitForExit();

            // Asenkron çıktıyı yakalamak için kısa bir bekleme ekle (Senkronizasyonu garantilemek için)
            await Task.Delay(100);

            // Asenkron akışları durdur
            process.CancelOutputRead();
            process.CancelErrorRead();

            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }
        catch (Exception ex)
        {
            // ... (hata yakalama)
            return (-1, "", ex.ToString());
        }
    }

    /// <summary>
    /// Builder container içinde migration'ı detached modda çalıştırır.
    /// </summary>

    public async Task TriggerBuilderMigrationAsync()
    {
        const string builderContainerName = "autoapi-builder";
        const string cmd = "dotnet /app/AutoAPI.Builder.dll migrate";
        const string command = $"docker exec {builderContainerName} {cmd}";

        _logger.LogInformation(">> Executing: {Command}", command);

        var (exitCode, output, error) = await RunCommandAsync(command);

        if (exitCode != 0)
        {
            _logger.LogError("❌ Builder migration trigger başarısız oldu. Çıktı:\n{Output}", output + error);
            throw new Exception($"Komut hatası ({exitCode}): {output + error}");
        }

        _logger.LogInformation("✅ Builder migration triggered. Output:\n{Output}", output);
    }
}
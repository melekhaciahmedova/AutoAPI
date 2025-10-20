using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AutoAPI.Core.Services;

public class MigrationWatcherService : BackgroundService
{
    private readonly ILogger<MigrationWatcherService> _logger;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly string _solutionRoot;
    private readonly string[] _watchPaths;
    private readonly int _debounceMs = 2500;
    private readonly object _debounceLock = new();
    private DateTime _lastChangeTime = DateTime.MinValue;
    private bool _migrationScheduled = false;

    public MigrationWatcherService(ILogger<MigrationWatcherService> logger)
    {
        _logger = logger;

        _solutionRoot = Directory.Exists("/src")
            ? "/src"
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        _logger.LogInformation("Solution root resolved as: {Root}", _solutionRoot);

        _watchPaths =
        [
            Path.Combine(_solutionRoot, "AutoAPI.Domain", "Entities"),
            Path.Combine(_solutionRoot, "AutoAPI.Data", "Infrastructure", "Configurations"),
            Path.Combine(_solutionRoot, "AutoAPI.Data", "Infrastructure")
        ];

        foreach (var path in _watchPaths)
        {
            Directory.CreateDirectory(path);
            var watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Renamed += OnChanged;
            _watchers.Add(watcher);

            _logger.LogInformation("Watching {Path}", path);
        }

        _logger.LogInformation("MigrationWatcherService running ({Ms}ms debounce)", _debounceMs);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("File change detected: {File}", e.FullPath);
        lock (_debounceLock)
        {
            _lastChangeTime = DateTime.Now;
            if (!_migrationScheduled)
            {
                _migrationScheduled = true;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_debounceMs);
                    await CheckForDebounceAsync();
                });
            }
        }
    }

    private async Task CheckForDebounceAsync()
    {
        bool shouldTrigger = false;
        lock (_debounceLock)
        {
            if (DateTime.Now - _lastChangeTime >= TimeSpan.FromMilliseconds(_debounceMs))
            {
                shouldTrigger = true;
                _migrationScheduled = false;
            }
        }

        if (shouldTrigger)
        {
            _logger.LogInformation("File changes stabilized, starting EF migration...");
            await GenerateMigrationAsync();
        }
    }

    private async Task GenerateMigrationAsync()
    {
        var migrationName = $"Auto_{DateTime.Now:yyyyMMdd_HHmmss}";
        var workingDir = _solutionRoot;

        // ✅ GÜNCELLEME: 'ef' komutu argümandan çıkarıldı.
        var args = $"migrations add {migrationName} --project \"/src/AutoAPI.Data\" --startup-project \"/src/AutoAPI.API\"";
        var dbUpdateCmd = $"database update --project \"/src/AutoAPI.Data\" --startup-project \"/src/AutoAPI.API\"";

        // ✅ GÜNCELLEME: Çalıştırılacak dosya 'dotnet-ef' olarak belirlendi.
        const string efExecutable = "dotnet-ef";

        _logger.LogInformation("EF Working Directory: {Dir}", workingDir);

        // API projesi çalışıyorsa kapat
        KillProcessByName("AutoAPI.API");

        _logger.LogInformation("Executing: {Exec} {Args}", efExecutable, args);
        // ✅ GÜNCELLEME: 'dotnet' yerine 'dotnet-ef' ile çağrılıyor.
        var (exitCode, output, error) = await RunAsync(efExecutable, args, workingDir);

        if (exitCode == 0)
        {
            _logger.LogInformation("Migration {Name} created successfully.", migrationName);

            _logger.LogInformation("Applying database update...");
            // ✅ GÜNCELLEME: 'dotnet' yerine 'dotnet-ef' ile çağrılıyor.
            var (dbCode, dbOut, dbErr) = await RunAsync(efExecutable, dbUpdateCmd, workingDir);

            if (dbCode == 0)
                _logger.LogInformation("✅ Database updated successfully.");
            else
                _logger.LogError("❌ Database update failed:\n{Output}", dbOut + dbErr);
        }
        else
        {
            _logger.LogError("Migration {Name} creation failed.", migrationName);
            _logger.LogInformation("EF Output:\n{Output}", output + error);
        }
    }

    private static async Task<(int exitCode, string output, string error)> RunAsync(string file, string args, string workingDir)
    {
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        // 🔥 1️⃣ DOTNET ortamını garanti et
        string dotnetRoot = "/usr/share/dotnet";
        string efToolsPath = "/root/.dotnet/tools";

        process.StartInfo.EnvironmentVariables["DOTNET_ROOT"] = dotnetRoot;
        process.StartInfo.EnvironmentVariables["PATH"] =
            $"{dotnetRoot}:{efToolsPath}:{Environment.GetEnvironmentVariable("PATH")}";

        // 🔥 2️⃣ kontrol log’u (debug için)
        Console.WriteLine($"[ENV] PATH={process.StartInfo.EnvironmentVariables["PATH"]}");

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[EF OUT] {e.Data}");
                outputBuilder.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.Error.WriteLine($"[EF ERR] {e.Data}");
                errorBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            string output = outputBuilder.ToString();
            string error = errorBuilder.ToString();

            Console.WriteLine($"[EF DONE] Exit Code: {process.ExitCode}");
            return (process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EF FAIL] {ex.Message}");
            return (-1, "", ex.ToString());
        }
    }

    private void KillProcessByName(string processName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                _logger.LogInformation("[PRE-BUILD] No running process found: {Name}", processName);
                return;
            }

            foreach (var proc in processes)
            {
                _logger.LogInformation("[PRE-BUILD] Killing process {Name} ({Id})", proc.ProcessName, proc.Id);
                proc.Kill(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Process kill işlemi başarısız oldu.");
        }
    }

    public async Task TriggerManualMigrationAsync() // ✅ Async yapıldı
    {
        _logger.LogInformation("Manual migration trigger received (sync waiting)...");

        // ✅ GÜNCELLEME: Task.Run yerine doğrudan GenerateMigrationAsync'i bekle
        await GenerateMigrationAsync();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {Time}", DateTime.Now);
        return Task.CompletedTask;
    }
}

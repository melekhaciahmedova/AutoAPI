using System.Diagnostics;

namespace AutoAPI.API.Services.Generation
{
    public class MigrationGeneratorService(string contentRootPath)
    {
        private readonly string _contentRootPath = contentRootPath;
        private const string ApiProjectName = "AutoAPI.API";
        private const string DataProjectName = "AutoAPI.Data";
        private const string MigrationsFolder = "Migrations";
        public async Task<string> AddMigrationAsync(string migrationName)
        {
            var apiProjectFileFullPath = Path.Combine(_contentRootPath, $"{ApiProjectName}.csproj");
            var solutionDirectory = Directory.GetParent(_contentRootPath)?.FullName;
            var dataProjectFileFullPath = Path.Combine(solutionDirectory, DataProjectName, $"{DataProjectName}.csproj");
            var workingDirectory = solutionDirectory;

            string outputDirRelative = MigrationsFolder;

            string arguments = $"ef migrations add {migrationName} " +
                                $"--project \"{dataProjectFileFullPath}\" " +
                                $"--startup-project \"{apiProjectFileFullPath}\" " +
                                $"--output-dir {outputDirRelative}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode != 0)
            {
                return $"Geçiş oluşturulurken KRİTİK HATA oluştu (Exit Code: {process.ExitCode}). Detay: {error} {output}";
            }

            return $"'{migrationName}' geçişi başarıyla oluşturuldu. Uygulama yeniden başlatılmalı.";
        }
    }
}
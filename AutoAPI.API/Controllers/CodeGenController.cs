using AutoAPI.Core.Generation;
using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Loader;

namespace AutoAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodeGenController(ITemplateRenderer renderer, IWebHostEnvironment env, ILogger<CodeGenController> logger) : ControllerBase
    {
        private readonly ITemplateRenderer _renderer = renderer;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<CodeGenController> _logger = logger;

        public record ApiResult(bool Success, string Message, List<object> Steps, string? Error = null);

        [HttpPost("generate-entity")]
        public async Task<IActionResult> GenerateEntity([FromBody] ClassDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ClassName))
                return BadRequest(new ApiResult(false, "Invalid class definition.", []));

            _logger.LogInformation("Entity generation started: {ClassName}", definition.ClassName);
            var steps = new List<object>();

            try
            {
                var entityGenerator = new EntityGeneratorService(_renderer, _env.ContentRootPath);
                var fluentGenerator = new FluentApiGeneratorService(_renderer, _env.ContentRootPath);
                var dbContextGenerator = new AppDbContextGeneratorService(_renderer, _env.ContentRootPath);

                await Task.WhenAll(
                    RunStepAsync("Entity Generation",
                        () => entityGenerator.GenerateEntitiesAsync([definition]), steps),
                    RunStepAsync("Fluent Configuration",
                        () => fluentGenerator.GenerateFluentConfigurationsAsync([definition]), steps)
                );

                await RunStepAsync("AppDbContext Generation",
                    () => dbContextGenerator.GenerateAppDbContextAsync([definition]), steps);

                _logger.LogInformation("Entity generation completed for {ClassName}", definition.ClassName);

                return Ok(new ApiResult(true,
                    $"{definition.ClassName} successfully generated!", steps));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity generation failed");
                var errorMsg = _env.IsDevelopment() ? ex.ToString() : ex.Message;
                return StatusCode(500,
                    new ApiResult(false, "Entity generation failed.", steps, errorMsg));
            }
        }

        private async Task RunStepAsync(string stepName, Func<Task> action, List<object> steps)
        {
            try
            {
                _logger.LogInformation("▶️ {StepName} started...", stepName);
                await action();
                steps.Add(new { step = stepName, status = "success" });
                _logger.LogInformation("{StepName} completed.", stepName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{StepName} failed.", stepName);
                steps.Add(new { step = stepName, status = "failed", error = ex.Message });
                throw;
            }
        }

        [HttpGet("check")]
        public IActionResult CheckEntity([FromQuery] string entityName)
        {
            try
            {
                var possiblePaths = new[]
                {
                    "/src/AutoAPI.Domain/bin/Release/net8.0/AutoAPI.Domain.dll",
                    "/src/AutoAPI.Domain/bin/Debug/net8.0/AutoAPI.Domain.dll",
                    Path.Combine(AppContext.BaseDirectory, "AutoAPI.Domain.dll"),
                    "/app/AutoAPI.Domain.dll"
                };

                string domainAssemblyPath = possiblePaths.FirstOrDefault(System.IO.File.Exists)
                    ?? throw new FileNotFoundException("AutoAPI.Domain.dll not found.");

                var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
                var assembly = context.LoadFromAssemblyPath(domainAssemblyPath);
                var entityType = assembly.GetType($"AutoAPI.Domain.Entities.{entityName}");

                if (entityType != null)
                {
                    return Ok(new
                    {
                        message = $"'{entityName}' sınıfı bulundu.",
                        fullName = entityType.FullName,
                        location = domainAssemblyPath
                    });
                }

                return NotFound(new
                {
                    message = $"'{entityName}' sınıfı bulunamadı (derlenmemiş veya namespace hatalı).",
                    searchedIn = domainAssemblyPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity kontrol hatası");
                return StatusCode(500, new
                {
                    message = "Kontrol sırasında hata oluştu",
                    error = ex.Message
                });
            }
        }
    }
}
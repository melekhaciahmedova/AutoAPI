using AutoAPI.Core.Generation;
using AutoAPI.Core.Services;
using AutoAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Loader;

namespace AutoAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodeGenController(
        ITemplateRenderer renderer,
        IWebHostEnvironment env,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<CodeGenController> logger
    ) : ControllerBase
    {
        private readonly ITemplateRenderer _renderer = renderer;
        private readonly IWebHostEnvironment _env = env;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<CodeGenController> _logger = logger;

        [HttpPost("generate-entity")]
        public async Task<IActionResult> GenerateEntity([FromBody] ClassDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ClassName))
                return BadRequest("Invalid class definition.");

            var steps = new List<object>();
            _logger.LogInformation($"🧱 Entity generation started: {definition.ClassName}");

            try
            {
                // 1️⃣ Entity class oluşturma
                _logger.LogInformation("📁 Step 1: Entity class generation started...");
                var entityGenerator = new EntityGeneratorService(_renderer, _env.ContentRootPath);
                await entityGenerator.GenerateEntitiesAsync([definition]);
                _logger.LogInformation("✅ Entity class generated successfully.");
                steps.Add(new { step = "Entity Generation", status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Entity generation failed.");
                steps.Add(new { step = "Entity Generation", status = "failed", error = ex.Message });
                return StatusCode(500, new { message = "❌ Entity generation failed.", steps });
            }

            try
            {
                // 2️⃣ Fluent API config oluşturma
                _logger.LogInformation("⚙️ Step 2: Fluent configuration generation started...");
                var fluentGenerator = new FluentApiGeneratorService(_renderer, _env.ContentRootPath);
                await fluentGenerator.GenerateFluentConfigurationsAsync([definition]);
                _logger.LogInformation("✅ Fluent configuration generated successfully.");
                steps.Add(new { step = "Fluent Configuration", status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fluent configuration generation failed.");
                steps.Add(new { step = "Fluent Configuration", status = "failed", error = ex.Message });
                return StatusCode(500, new { message = "❌ Fluent configuration generation failed.", steps });
            }

            try
            {
                // 3️⃣ AppDbContext güncelleme
                _logger.LogInformation("🧩 Step 3: AppDbContext generation started...");
                var dbContextGenerator = new AppDbContextGeneratorService(_renderer, _env.ContentRootPath);
                await dbContextGenerator.GenerateAppDbContextAsync([definition]);
                _logger.LogInformation("✅ AppDbContext updated successfully.");
                steps.Add(new { step = "AppDbContext Generation", status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AppDbContext generation failed.");
                steps.Add(new { step = "AppDbContext Generation", status = "failed", error = ex.Message });
                return StatusCode(500, new { message = "❌ AppDbContext generation failed.", steps });
            }

            _logger.LogInformation($"🎯 Entity generation completed successfully for {definition.ClassName}");

            return Ok(new
            {
                message = $"✅ {definition.ClassName} successfully generated!",
                steps
            });
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
                    ?? throw new FileNotFoundException("AutoAPI.Domain.dll bulunamadı. Derlenmiş dosya mevcut değil.");

                // ✅ İzole context kullan
                var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
                var assembly = context.LoadFromAssemblyPath(domainAssemblyPath);
                var entityType = assembly.GetType($"AutoAPI.Domain.Entities.{entityName}");

                if (entityType != null)
                {
                    return Ok(new
                    {
                        message = $"✅ '{entityName}' sınıfı bulundu.",
                        fullName = entityType.FullName,
                        location = domainAssemblyPath
                    });
                }

                return NotFound(new
                {
                    message = $"❌ '{entityName}' sınıfı bulunamadı (derlenmemiş veya namespace hatalı).",
                    searchedIn = domainAssemblyPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity kontrol hatası");
                return StatusCode(500, new
                {
                    message = "🔥 Kontrol sırasında hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpGet("check-dbcontext")]
        public IActionResult CheckDbContext()
        {
            try
            {
                // Aranabilecek olası derlenmiş AutoAPI.Data.dll yolları
                var possiblePaths = new[]
                {
                    "/src/AutoAPI.Data/bin/Release/net8.0/AutoAPI.Data.dll",
                    "/src/AutoAPI.Data/bin/Debug/net8.0/AutoAPI.Data.dll",
                    Path.Combine(AppContext.BaseDirectory, "AutoAPI.Data.dll"),
                    "/app/AutoAPI.Data.dll"
                };

                string dataAssemblyPath = possiblePaths.FirstOrDefault(System.IO.File.Exists)
                    ?? throw new FileNotFoundException("AutoAPI.Data.dll bulunamadı. Derlenmiş dosya mevcut değil.");

                // İzole context'te yükle
                var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
                var assembly = context.LoadFromAssemblyPath(dataAssemblyPath);

                // AppDbContext türünü bul
                var dbContextType = assembly.GetType("AutoAPI.Data.Infrastructure.AppDbContext");

                if (dbContextType != null)
                {
                    // ✅ AppDbContext bulundu
                    return Ok(new
                    {
                        message = "✅ AppDbContext başarıyla bulundu.",
                        fullName = dbContextType.FullName,
                        assembly = dataAssemblyPath,
                        properties = dbContextType.GetProperties().Select(p => new
                        {
                            p.Name,
                            PropertyType = p.PropertyType.Name
                        }).ToList()
                    });
                }

                // ❌ Bulunamadıysa detay ver
                return NotFound(new
                {
                    message = "❌ AppDbContext sınıfı bulunamadı (derlenmemiş, namespace hatalı veya eksik).",
                    searchedIn = dataAssemblyPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AppDbContext kontrol hatası");
                return StatusCode(500, new
                {
                    message = "🔥 AppDbContext kontrolü sırasında hata oluştu.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("migrate")]
        public async Task<IActionResult> MigrateAsync()
        {
            var orchestratorUrl =
                Environment.GetEnvironmentVariable("ORCHESTRATOR_URL")
                ?? _configuration["Orchestrator:Url"]
                ?? "http://autoapi-orchestrator:8080/api/orchestrator/trigger";

            _logger.LogInformation($"🚀 Triggering migration via Orchestrator: {orchestratorUrl}");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(orchestratorUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Migration trigger sent successfully.");
                    return Ok(new { message = "Migration trigger sent successfully." });
                }
                else
                {
                    _logger.LogWarning($"⚠️ Migration trigger failed. StatusCode: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, new { message = "Migration trigger failed." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while sending migration trigger.");
                return StatusCode(500, new { message = "Error sending migration trigger", error = ex.Message });
            }
        }
    }
}
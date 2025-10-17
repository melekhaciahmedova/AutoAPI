using AutoAPI.API.Services;
using AutoAPI.API.Services.Generation;
using AutoAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

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

        // ============================================================
        // 1️⃣ Entity oluşturma
        // ============================================================
        [HttpPost("generate-entity")]
        public async Task<IActionResult> GenerateEntity([FromBody] ClassDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ClassName))
                return BadRequest("Invalid class definition.");

            _logger.LogInformation($"🧱 Generating entity: {definition.ClassName}");

            var entityGenerator = new EntityGeneratorService(_renderer, _env.ContentRootPath);
            await entityGenerator.GenerateEntitiesAsync([definition]);

            var fluentGenerator = new FluentApiGeneratorService(_renderer, _env.ContentRootPath);
            await fluentGenerator.GenerateFluentConfigurationsAsync([definition]);

            var dbContextGenerator = new AppDbContextGeneratorService(_renderer, _env.ContentRootPath);
            await dbContextGenerator.GenerateAppDbContextAsync([definition]);

            return Ok($"{definition.ClassName} successfully generated!");
        }

        // ============================================================
        // 2️⃣ Entity kontrol etme
        // ============================================================
        [HttpGet("check")]
        public IActionResult CheckEntity([FromQuery] string entityName)
        {
            try
            {
                var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
                var domainAssemblyPath = Path.Combine(solutionRoot, "AutoAPI.Domain", "bin", "Debug", "net8.0", "AutoAPI.Domain.dll");

                if (!System.IO.File.Exists(domainAssemblyPath))
                    return NotFound($"❌ Domain assembly bulunamadı: {domainAssemblyPath}");

                var assembly = Assembly.LoadFrom(domainAssemblyPath);
                var entityType = assembly.GetType($"AutoAPI.Domain.Entities.{entityName}");

                if (entityType != null)
                    return Ok($"✅ '{entityName}' sınıfı bulundu: {entityType.FullName}");
                else
                    return NotFound($"❌ '{entityName}' sınıfı bulunamadı (derlenmemiş veya namespace hatalı).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Entity kontrol hatası");
                return StatusCode(500, $"🔥 Kontrol sırasında hata oluştu: {ex.Message}");
            }
        }

        // ============================================================
        // 3️⃣ Migration tetikleme (Orchestrator üzerinden)
        // ============================================================
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
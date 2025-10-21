using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TriggerController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<TriggerController> logger) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<TriggerController> _logger = logger;

        [HttpPost("migrate")]
        public async Task<IActionResult> MigrateAsync()
        {
            var orchestratorUrl =
                Environment.GetEnvironmentVariable("ORCHESTRATOR_URL")
                ?? _configuration["Orchestrator:Url"]
                ?? "http://autoapi-orchestrator:8080/api/orchestrator/trigger";

            _logger.LogInformation($"Triggering migration via Orchestrator: {orchestratorUrl}");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(orchestratorUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Migration trigger sent successfully.");
                    return Ok(new { message = "Migration trigger sent successfully." });
                }
                else
                {
                    _logger.LogWarning($"Migration trigger failed. StatusCode: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, new { message = "Migration trigger failed." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending migration trigger.");
                return StatusCode(500, new { message = "Error sending migration trigger", error = ex.Message });
            }
        }
    }
}
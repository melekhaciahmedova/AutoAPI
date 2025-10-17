using AutoAPI.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoAPI.Orchestrator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestratorController(DockerService docker, ILogger<OrchestratorController> logger) : ControllerBase
{
    private readonly DockerService _docker = docker;
    private readonly ILogger<OrchestratorController> _logger = logger;

    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerBuilderAsync()
    {
        _logger.LogInformation("Migration trigger request received via Orchestrator...");

        try
        {
            await _docker.TriggerBuilderMigrationAsync();
            return Ok(new { message = "Builder migration triggered successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Builder migration failed.");
            return StatusCode(500, new { message = "Migration failed.", error = ex.Message });
        }
    }
}
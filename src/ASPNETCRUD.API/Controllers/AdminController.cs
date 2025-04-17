using ASPNETCRUD.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNETCRUD.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataSeeder _seeder;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;

        public AdminController(
            DataSeeder seeder, 
            ILogger<AdminController> logger,
            IConfiguration configuration)
        {
            _seeder = seeder;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("reset-demo-data")]
        public async Task<IActionResult> ResetDemoData([FromHeader(Name = "X-API-Key")] string apiKey)
        {
            // Verify API key for authorization
            var configuredApiKey = _configuration["DemoSettings:ApiKey"];
            if (string.IsNullOrEmpty(configuredApiKey) || apiKey != configuredApiKey)
            {
                _logger.LogWarning("Unauthorized reset attempt with invalid API key");
                return Unauthorized(new { message = "Invalid API key" });
            }

            try
            {
                _logger.LogInformation("Starting demo data reset");
                await _seeder.SeedDemoDataAsync();
                _logger.LogInformation("Demo data reset completed successfully");
                return Ok(new { message = "Demo data reset successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting demo data");
                return StatusCode(500, new { message = "Error resetting demo data", error = ex.Message });
            }
        }

        [HttpPost("cron-reset")]
        public async Task<IActionResult> CronReset()
        {
            // Check if this is a Railway cron job
            var isCronJob = Environment.GetEnvironmentVariable("RAILWAY_CRON_JOB");
            if (string.IsNullOrEmpty(isCronJob))
            {
                _logger.LogWarning("Unauthorized cron-reset attempt from non-cron environment");
                return Unauthorized(new { message = "This endpoint can only be called from a Railway cron job" });
            }

            try
            {
                _logger.LogInformation("Starting cron-triggered database reset");
                await _seeder.SeedDemoDataAsync();
                _logger.LogInformation("Cron-triggered database reset completed successfully");
                return Ok(new { message = "Database reset successful", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cron-triggered database reset");
                return StatusCode(500, new { message = "Error resetting database", error = ex.Message });
            }
        }
    }
} 
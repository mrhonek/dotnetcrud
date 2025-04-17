using ASPNETCRUD.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace ASPNETCRUD.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataSeeder _seeder;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private static readonly SemaphoreSlim _resetLock = new SemaphoreSlim(1, 1);
        private static bool _isResetting = false;

        public AdminController(
            DataSeeder seeder, 
            ILogger<AdminController> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _seeder = seeder;
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
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
        public IActionResult CronReset()
        {
            try
            {
                _logger.LogInformation("Cron reset endpoint called at {Time}", DateTime.UtcNow);
                
                // Check if a reset is already in progress
                if (_isResetting)
                {
                    _logger.LogWarning("Reset already in progress, skipping new request");
                    return StatusCode(429, "Reset operation already in progress");
                }

                // Start the reset process in a background thread
                // Use a captured service provider to create a new scope
                ThreadPool.QueueUserWorkItem(async _ => 
                {
                    try 
                    {
                        if (await _resetLock.WaitAsync(0)) // Try to acquire lock without waiting
                        {
                            try
                            {
                                _isResetting = true;
                                _logger.LogInformation("Starting database reset in background thread at {Time}", DateTime.UtcNow);
                                
                                // Create a new scope to get fresh instances of all services
                                using (var scope = _serviceProvider.CreateScope())
                                {
                                    // Get a new instance of DataSeeder within this scope
                                    var scopedSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                                    
                                    // Use the logger from the controller since it's typically a singleton
                                    _logger.LogInformation("Running database seeding in background thread with new scope");
                                    
                                    // Execute the seeding operation
                                    await scopedSeeder.SeedDemoDataAsync();
                                    
                                    _logger.LogInformation("Database reset completed successfully at {Time}", DateTime.UtcNow);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error during database reset in background thread");
                            }
                            finally
                            {
                                _isResetting = false;
                                _resetLock.Release();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background thread management");
                        _isResetting = false;
                    }
                });

                return Ok("Database reset initiated. The operation will complete in the background.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cron reset endpoint");
                return StatusCode(500, "An error occurred during reset operation");
            }
        }
        
        // New public endpoint with simple auth for manual resets
        [HttpGet("manual-reset")]
        public IActionResult ManualReset([FromQuery] string key)
        {
            // Verify secret key for authorization
            var configuredApiKey = _configuration["DemoSettings:ApiKey"];
            if (string.IsNullOrEmpty(configuredApiKey) || key != configuredApiKey)
            {
                _logger.LogWarning("Unauthorized manual reset attempt with invalid key");
                return Unauthorized(new { message = "Invalid key" });
            }
            
            // Call the cron reset endpoint which handles background processing
            return CronReset();
        }
    }
} 
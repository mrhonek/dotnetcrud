using ASPNETCRUD.API.Services;
using Microsoft.Extensions.Logging;

namespace ASPNETCRUD.API.Jobs
{
    public class DatabaseResetJob
    {
        private readonly DataSeeder _seeder;
        private readonly ILogger<DatabaseResetJob> _logger;

        public DatabaseResetJob(DataSeeder seeder, ILogger<DatabaseResetJob> logger)
        {
            _seeder = seeder;
            _logger = logger;
        }

        public async Task ResetDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting scheduled database reset");
                await _seeder.SeedDemoDataAsync();
                _logger.LogInformation("Scheduled database reset completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled database reset");
            }
        }
    }
} 
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
                _logger.LogInformation("Starting scheduled database reset at: {Time}", DateTime.UtcNow);
                
                // Log environment information
                _logger.LogInformation("Environment info - ASPNETCORE_ENVIRONMENT: {Env}", 
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set");
                
                _logger.LogInformation("Environment info - RAILWAY_CRON_JOB: {Cron}", 
                    Environment.GetEnvironmentVariable("RAILWAY_CRON_JOB") ?? "Not set");
                
                // Log connection string (excluding password)
                var connString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "Not set";
                if (connString != "Not set")
                {
                    var sanitized = SanitizeConnectionString(connString);
                    _logger.LogInformation("Using connection string: {ConnString}", sanitized);
                }
                
                await _seeder.SeedDemoDataAsync();
                _logger.LogInformation("Scheduled database reset completed successfully at: {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled database reset: {ErrorMessage}", ex.Message);
                
                // Log inner exceptions if any
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    _logger.LogError("Inner exception: {ErrorMessage}", innerEx.Message);
                    innerEx = innerEx.InnerException;
                }
            }
        }
        
        // Helper method to sanitize connection string for logging
        private string SanitizeConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;
                
            // Replace password with asterisks
            if (connectionString.Contains("Password="))
            {
                var parts = connectionString.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                    {
                        parts[i] = "Password=*****";
                    }
                }
                return string.Join(";", parts);
            }
            
            return connectionString;
        }
    }
} 
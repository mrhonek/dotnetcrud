using ASPNETCRUD.API.Middleware;
using ASPNETCRUD.API.Services;
using ASPNETCRUD.API.Jobs;
using ASPNETCRUD.Application;
using ASPNETCRUD.Infrastructure;
using Microsoft.OpenApi.Models;
using System.IO;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

// Check for database reset command
bool resetDatabase = args.Contains("--reset-database");

// Load environment variables
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // Explicitly load environment variables

// Add rate limiting services
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

// Add services to the container
builder.Services.AddControllers();

// Add logging with debug level for auth-related categories
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add temporary debug logs
builder.Logging.AddFilter("ASPNETCRUD.API.Controllers.AuthController", LogLevel.Debug);
builder.Logging.AddFilter("ASPNETCRUD.Infrastructure.Services.AuthService", LogLevel.Debug);

// Register the DataSeeder and DatabaseResetJob
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<DatabaseResetJob>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ASPNETCRUD API", 
        Version = "v1",
        Description = "📢 DEMO PROJECT - DATA RESETS DAILY 📢\n\nA demo API showcasing ASP.NET Core and Clean Architecture. This is a portfolio project with data that resets daily at 2 AM UTC.\n\nDemo Credentials:\n- Admin: demo / Demo@123\n- User: user / User@123",
        Contact = new OpenApiContact
        {
            Name = "Portfolio Demo Project",
            Url = new Uri("https://github.com/mrhonek/dotnetcrud")
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Application Layer
builder.Services.AddApplication();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// If reset database command was passed, run the reset and exit
if (resetDatabase)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var resetJob = scope.ServiceProvider.GetRequiredService<DatabaseResetJob>();
    
    logger.LogInformation("Database reset command detected");
    
    try
    {
        resetJob.ResetDatabaseAsync().GetAwaiter().GetResult();
        logger.LogInformation("Database reset completed successfully");
        return 0; // Exit with success code
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error resetting database");
        return 1; // Exit with error code
    }
}

// Configure the HTTP request pipeline
// Apply rate limiting
app.UseIpRateLimiting();

// Apply Swagger basic auth
app.UseMiddleware<SwaggerBasicAuthMiddleware>();

// Enable Swagger in all environments for demonstration purposes
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// TEMPORARY FOR DEBUGGING: Add logging middleware to log all requests/responses
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    // Log the request
    logger.LogInformation("Request: {Method} {Path}{QueryString}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString);
    
    // For POST/PUT requests, try to log the body
    if ((context.Request.Method == "POST" || context.Request.Method == "PUT") && 
        context.Request.Path.ToString().Contains("/Auth/"))
    {
        try
        {
            // Enable request body reading
            context.Request.EnableBuffering();
            
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    // Log the body but strip out password fields for security
                    var sanitizedBody = body.Replace("\"password\":\"[^\"]*\"", "\"password\":\"[REDACTED]\"")
                                          .Replace("\"confirmPassword\":\"[^\"]*\"", "\"confirmPassword\":\"[REDACTED]\"");
                    logger.LogDebug("Request body: {Body}", sanitizedBody);
                }
                
                // Reset the position to allow reading again
                context.Request.Body.Position = 0;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading request body");
        }
    }
    
    await next();
});

// Still use the custom exception handler but with our enhanced error details
app.UseCustomExceptionHandler();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run EF Core migrations automatically with better error logging
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database initialization");
        var context = services.GetRequiredService<ASPNETCRUD.Infrastructure.Data.ApplicationDbContext>();
        logger.LogInformation("Database context created");
        
        logger.LogInformation("Ensuring database is created");
        var dbCreated = context.Database.EnsureCreated();
        logger.LogInformation("Database created: {Created}", dbCreated);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

// Map health checks endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(
            new 
            { 
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new 
                { 
                    name = e.Key, 
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            });
        
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(result);
    }
});

app.Run();
return 0; // Add return at the end of the program 
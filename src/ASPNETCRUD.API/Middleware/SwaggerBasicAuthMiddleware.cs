using System.Net;
using System.Text;

namespace ASPNETCRUD.API.Middleware
{
    public class SwaggerBasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to Swagger paths
            if (IsSwaggerRequest(context.Request.Path))
            {
                string? authHeader = context.Request.Headers["Authorization"];
                
                // Check if credentials are provided
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
                {
                    SetUnauthorizedResponse(context);
                    return;
                }

                // Validate credentials
                var encodedCredentials = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Length > 1 
                    ? authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim() 
                    : null;
                
                if (string.IsNullOrEmpty(encodedCredentials))
                {
                    SetUnauthorizedResponse(context);
                    return;
                }

                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                
                if (credentials.Length != 2)
                {
                    SetUnauthorizedResponse(context);
                    return;
                }
                
                var username = credentials[0];
                var password = credentials[1];
                
                // Get credentials from configuration
                var configUsername = _configuration["SwaggerAuth:Username"] ?? "portfolio";
                var configPassword = _configuration["SwaggerAuth:Password"] ?? "demo2024";
                
                if (username != configUsername || password != configPassword)
                {
                    SetUnauthorizedResponse(context);
                    return;
                }
            }
            
            await _next.Invoke(context);
        }

        private static bool IsSwaggerRequest(PathString path)
        {
            return path.StartsWithSegments("/swagger");
        }

        private static void SetUnauthorizedResponse(HttpContext context)
        {
            context.Response.Headers["WWW-Authenticate"] = "Basic";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
    }
} 
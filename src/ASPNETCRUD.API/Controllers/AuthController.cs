using ASPNETCRUD.Application.DTOs;
using ASPNETCRUD.Application.Interfaces;
using ASPNETCRUD.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ASPNETCRUD.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            _logger.LogInformation("Registration attempt for user: {Username}, Email: {Email}", 
                registerDto.Username, registerDto.Email);
                
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for registration: {ModelState}", 
                        JsonSerializer.Serialize(ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Model state valid, proceeding with registration");
                var result = await _authService.RegisterAsync(registerDto);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Registration failed: {Errors}", 
                        string.Join(", ", result.Errors));
                    return BadRequest(result);
                }

                _logger.LogInformation("Registration successful for {Username}", registerDto.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for {Username}", registerDto.Username);
                throw; // Let the middleware handle it
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);

            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto);

            if (!result.IsSuccess)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeToken()
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var result = await _authService.RevokeTokenAsync(username);

            if (!result)
            {
                return BadRequest(new { message = "Failed to revoke token" });
            }

            return NoContent();
        }

        [HttpGet("diagnostic")]
        public ActionResult<object> RunDiagnostic()
        {
            var diagnosticResults = new Dictionary<string, object>();
            
            try
            {
                // Check JWT configuration more simply
                _logger.LogInformation("Checking JWT configuration");
                var jwtConfigured = false;
                
                try {
                    jwtConfigured = _authService.IsJwtConfigured();
                    diagnosticResults.Add("JwtConfigured", jwtConfigured);
                }
                catch (Exception jwtEx) {
                    diagnosticResults.Add("JwtError", jwtEx.Message);
                }
                
                // Check database connection
                var dbResult = new Dictionary<string, object>();
                try
                {
                    var testData = _authService.TestDatabaseConnection();
                    dbResult.Add("Connected", true);
                    dbResult.Add("UserCount", testData.UserCount);
                    
                    // Avoid null reference by using null conditional and null coalescing operators
                    dbResult.Add("DatabaseName", testData.DatabaseName ?? "Unknown");
                    diagnosticResults.Add("Database", dbResult);
                }
                catch (Exception dbEx)
                {
                    dbResult.Add("Connected", false);
                    dbResult.Add("Error", dbEx.Message ?? "Unknown error");
                    
                    // Avoid null reference with null conditional and null coalescing operators
                    dbResult.Add("InnerError", dbEx.InnerException?.Message ?? "No inner exception");
                    diagnosticResults.Add("Database", dbResult);
                }
                
                diagnosticResults.Add("Success", true);
            }
            catch (Exception ex)
            {
                diagnosticResults.Add("Success", false);
                diagnosticResults.Add("Error", ex.Message ?? "Unknown error");
                diagnosticResults.Add("StackTrace", ex.StackTrace ?? "No stack trace available");
            }
            
            return Ok(diagnosticResults);
        }
    }
} 
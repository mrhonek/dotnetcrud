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
                // More detailed model logging
                _logger.LogDebug("Registration request details: {@RegisterData}", 
                    new 
                    { 
                        Username = registerDto.Username, 
                        Email = registerDto.Email, 
                        FirstName = registerDto.FirstName, 
                        LastName = registerDto.LastName,
                        HasPassword = !string.IsNullOrEmpty(registerDto.Password),
                        HasConfirmPassword = !string.IsNullOrEmpty(registerDto.ConfirmPassword),
                        PasswordsMatch = registerDto.Password == registerDto.ConfirmPassword
                    });
                
                // Manual validation check to catch potential model binding issues
                var validationIssues = new List<string>();
                
                if (string.IsNullOrWhiteSpace(registerDto.Username))
                    validationIssues.Add("Username is required");
                    
                if (string.IsNullOrWhiteSpace(registerDto.Email))
                    validationIssues.Add("Email is required");
                    
                if (string.IsNullOrWhiteSpace(registerDto.Password))
                    validationIssues.Add("Password is required");
                    
                if (!string.IsNullOrWhiteSpace(registerDto.Password) && 
                    string.IsNullOrWhiteSpace(registerDto.ConfirmPassword))
                    validationIssues.Add("Confirm Password is required");
                
                if (validationIssues.Count > 0)
                {
                    _logger.LogWarning("Manual validation failed: {Issues}", 
                        string.Join(", ", validationIssues));
                    
                    return BadRequest(new AuthResponseDto {
                        IsSuccess = false,
                        Message = "Validation failed",
                        Errors = validationIssues
                    });
                }
                
                // Check if model state is valid
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Model state validation failed: {Errors}", 
                        string.Join(", ", errors));
                        
                    return BadRequest(new AuthResponseDto {
                        IsSuccess = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                _logger.LogInformation("Model validation passed, proceeding with registration");
                var result = await _authService.RegisterAsync(registerDto);

                if (!result.IsSuccess)
                {
                    // Log all errors
                    _logger.LogWarning("Registration failed in service: {Message}. Errors: {Errors}", 
                        result.Message,
                        result.Errors.Count > 0 ? string.Join(", ", result.Errors) : "None");
                    
                    // Ensure there's always at least one error message to show the user
                    if (result.Errors.Count == 0 && !string.IsNullOrEmpty(result.Message))
                    {
                        result.Errors.Add(result.Message);
                    }
                    else if (result.Errors.Count == 0 && string.IsNullOrEmpty(result.Message))
                    {
                        result.Message = "Registration failed";
                        result.Errors.Add("An unknown error occurred");
                    }
                    
                    return BadRequest(result);
                }

                _logger.LogInformation("Registration successful for {Username}", registerDto.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for {Username}", registerDto.Username);
                
                // Return detailed error information in non-production environments
                var errorResponse = new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { ex.Message }
                };
                
                return BadRequest(errorResponse);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken()
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest(new { message = "Invalid token" });
            }

            var result = await _authService.RevokeTokenAsync(username);

            if (!result)
            {
                return BadRequest(new { message = "Token revocation failed" });
            }

            return Ok(new { message = "Token revoked successfully" });
        }

        [HttpGet("diagnostic")]
        [Authorize(Roles = "Admin")]
        public IActionResult RunDiagnostic()
        {
            try
            {
                var isJwtConfigured = _authService.IsJwtConfigured();
                _logger.LogInformation("JWT configured: {IsConfigured}", isJwtConfigured);
                
                var dbTestResult = _authService.TestDatabaseConnection();
                _logger.LogInformation("Database connection test: Database={DatabaseName}, UserCount={UserCount}", 
                    dbTestResult.DatabaseName, dbTestResult.UserCount);
                
                return Ok(new
                {
                    JwtConfigured = isJwtConfigured,
                    DatabaseName = dbTestResult.DatabaseName,
                    UserCount = dbTestResult.UserCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during diagnostic check");
                return StatusCode(500, new { message = "Error during diagnostic check", error = ex.Message });
            }
        }
    }
} 
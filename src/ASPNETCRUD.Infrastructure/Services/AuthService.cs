using AutoMapper;
using ASPNETCRUD.Application.DTOs;
using ASPNETCRUD.Application.Interfaces;
using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;
using ASPNETCRUD.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ASPNETCRUD.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly TokenGeneratorService _tokenGenerator;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            TokenGeneratorService tokenGenerator,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tokenGenerator = tokenGenerator;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var response = new AuthResponseDto();

            var user = await _unitOfWork.Users.GetByUsernameAsync(loginDto.Username);
            
            if (user == null)
            {
                response.IsSuccess = false;
                response.Message = "Invalid username or password";
                return response;
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            
            if (!isPasswordValid)
            {
                response.IsSuccess = false;
                response.Message = "Invalid username or password";
                return response;
            }

            // Generate tokens
            var token = _tokenGenerator.GenerateJwtToken(user);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();

            // Save refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();

            response.IsSuccess = true;
            response.Token = token;
            response.RefreshToken = refreshToken;
            response.Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
            
            return response;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Starting registration process for {Username}", registerDto.Username);
            var response = new AuthResponseDto();

            try
            {
                // Log registration data for debugging (in dev environments)
                #if DEBUG
                _logger.LogDebug("Registration data: {Data}", JsonSerializer.Serialize(registerDto, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
                #endif

                // Validate unique username and email
                _logger.LogInformation("Checking if username {Username} is unique", registerDto.Username);
                var isUsernameUnique = await _unitOfWork.Users.IsUsernameUniqueAsync(registerDto.Username);
                if (!isUsernameUnique)
                {
                    _logger.LogWarning("Username {Username} is already taken", registerDto.Username);
                    response.IsSuccess = false;
                    response.Errors.Add("Username is already taken");
                }

                _logger.LogInformation("Checking if email {Email} is unique", registerDto.Email);
                var isEmailUnique = await _unitOfWork.Users.IsEmailUniqueAsync(registerDto.Email);
                if (!isEmailUnique)
                {
                    _logger.LogWarning("Email {Email} is already registered", registerDto.Email);
                    response.IsSuccess = false;
                    response.Errors.Add("Email is already registered");
                }

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("Validation failed. Errors: {Errors}", 
                        string.Join(", ", response.Errors));
                    return response;
                }

                // Map and create user
                _logger.LogInformation("Mapping RegisterDto to User entity");
                var user = _mapper.Map<User>(registerDto);
                
                // Hash password
                _logger.LogInformation("Hashing password");
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
                
                // Add default role
                _logger.LogInformation("Adding default role 'User'");
                user.Roles.Add("User");

                // Create user in database
                _logger.LogInformation("Adding user to database");
                await _unitOfWork.Users.AddAsync(user);
                
                try
                {
                    _logger.LogInformation("Saving changes to database");
                    await _unitOfWork.CompleteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving user to database");
                    response.IsSuccess = false;
                    response.Message = "Error creating user in database";
                    response.Errors.Add(ex.Message);
                    return response;
                }

                // Generate tokens
                _logger.LogInformation("Generating JWT token and refresh token");
                var token = _tokenGenerator.GenerateJwtToken(user);
                var refreshToken = _tokenGenerator.GenerateRefreshToken();

                // Save refresh token
                _logger.LogInformation("Saving refresh token for user {Username}", user.Username);
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CompleteAsync();

                // Set response
                response.IsSuccess = true;
                response.Message = "Registration successful";
                response.Token = token;
                response.RefreshToken = refreshToken;
                response.Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
                
                _logger.LogInformation("Registration successful for {Username}", user.Username);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for {Username}", registerDto.Username);
                response.IsSuccess = false;
                response.Message = "An error occurred during registration";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var response = new AuthResponseDto();

            try
            {
                var principal = _tokenGenerator.GetPrincipalFromExpiredToken(refreshTokenDto.Token);
                var username = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(username))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid token";
                    return response;
                }

                var user = await _unitOfWork.Users.GetByUsernameAsync(username);

                if (user == null || 
                    user.RefreshToken != refreshTokenDto.RefreshToken || 
                    user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid refresh token or token expired";
                    return response;
                }

                // Generate new tokens
                var newToken = _tokenGenerator.GenerateJwtToken(user);
                var newRefreshToken = _tokenGenerator.GenerateRefreshToken();

                // Update refresh token
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.CompleteAsync();

                response.IsSuccess = true;
                response.Token = newToken;
                response.RefreshToken = newRefreshToken;
                response.Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
                
                return response;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = "Error refreshing token";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<bool> RevokeTokenAsync(string username)
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(username);
            if (user == null)
            {
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();
            
            return true;
        }

        public DatabaseTestResult TestDatabaseConnection()
        {
            try
            {
                _logger.LogInformation("Testing database connection");
                
                // Get the DbContext through the UnitOfWork and cast it to the correct type
                var dbContextObj = _unitOfWork.GetDbContext();
                
                if (dbContextObj == null)
                {
                    _logger.LogError("Could not access database context from unit of work");
                    throw new InvalidOperationException("Database context is not accessible");
                }
                
                // Cast to ApplicationDbContext
                var dbContext = dbContextObj as ApplicationDbContext;
                
                if (dbContext == null)
                {
                    _logger.LogError("Database context is not of type ApplicationDbContext");
                    throw new InvalidOperationException("Invalid database context type");
                }
                
                // Try a simple connection test
                var canConnect = dbContext.Database.CanConnect();
                _logger.LogInformation("Database connection test: {CanConnect}", canConnect);
                
                if (!canConnect)
                {
                    _logger.LogWarning("Database connection test failed");
                    throw new InvalidOperationException("Cannot connect to the database");
                }
                
                // Count users - safely
                int userCount = 0;
                try
                {
                    userCount = _unitOfWork.Users.GetAllAsync().Result.Count();
                    _logger.LogInformation("User count: {UserCount}", userCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting users");
                }
                
                // Get database name - more safely
                string databaseName = "Unknown";
                try
                {
                    // Option 1: Get from connection string (without exposing full connection string)
                    var connectionString = dbContext.Database.GetConnectionString();
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        // Parse database name from connection string safely (without showing the whole string)
                        var parts = connectionString.Split(';')
                            .Select(part => part.Trim())
                            .Where(part => part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault();

                        if (parts != null)
                        {
                            databaseName = parts.Substring("Database=".Length);
                        }
                    }
                    
                    // Option 2: Try to use the provider name as a fallback
                    if (databaseName == "Unknown")
                    {
                        databaseName = dbContext.Database.ProviderName?.Split('.').LastOrDefault() ?? "Unknown";
                    }
                    
                    _logger.LogInformation("Database name: {DatabaseName}", databaseName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting database name");
                }
                
                return new DatabaseTestResult
                {
                    UserCount = userCount,
                    DatabaseName = databaseName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                throw;
            }
        }

        public bool IsJwtConfigured()
        {
            try
            {
                _logger.LogInformation("Checking if JWT is configured");
                
                // Check if key exists and is not empty
                var hasKey = !string.IsNullOrEmpty(_jwtSettings.Key);
                _logger.LogInformation("JWT Key exists and is not empty: {HasKey}", hasKey);
                
                // Check if issuer exists
                var hasIssuer = !string.IsNullOrEmpty(_jwtSettings.Issuer);
                _logger.LogInformation("JWT Issuer exists and is not empty: {HasIssuer}", hasIssuer);
                
                // Check if audience exists
                var hasAudience = !string.IsNullOrEmpty(_jwtSettings.Audience);
                _logger.LogInformation("JWT Audience exists and is not empty: {HasAudience}", hasAudience);
                
                return hasKey && hasIssuer && hasAudience;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking JWT configuration");
                return false;
            }
        }
    }
} 
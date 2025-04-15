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
                
                // Get the connection string (safely, without exposing sensitive information)
                var connectionString = _unitOfWork.GetType()
                    .Assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.Name == "ApplicationDbContext")?
                    .GetProperty("Database")?
                    .GetValue(_unitOfWork.GetType()
                        .GetProperty("DbContext")?
                        .GetValue(_unitOfWork))?
                    .ToString();
                
                _logger.LogInformation("Database connection available: {ConnectionAvailable}", 
                    !string.IsNullOrEmpty(connectionString));
                
                // Count users
                var userCount = _unitOfWork.Users.GetAllAsync().Result.Count();
                _logger.LogInformation("User count: {UserCount}", userCount);
                
                // Get database name
                var dbContext = _unitOfWork.GetType()
                    .GetProperty("DbContext")?
                    .GetValue(_unitOfWork);
                
                var database = dbContext?.GetType()
                    .GetProperty("Database")?
                    .GetValue(dbContext);
                
                var databaseName = database?.GetType()
                    .GetMethod("GetDbConnection")?
                    .Invoke(database, null)?
                    .GetType()
                    .GetProperty("Database")?
                    .GetValue(database.GetType()
                        .GetMethod("GetDbConnection")?
                        .Invoke(database, null))
                    ?.ToString() ?? "Unknown";
                
                _logger.LogInformation("Database name: {DatabaseName}", databaseName);
                
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
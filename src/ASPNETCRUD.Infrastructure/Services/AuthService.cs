using AutoMapper;
using ASPNETCRUD.Application.DTOs;
using ASPNETCRUD.Application.Interfaces;
using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using BCrypt.Net;

namespace ASPNETCRUD.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly TokenGeneratorService _tokenGenerator;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            TokenGeneratorService tokenGenerator,
            IOptions<JwtSettings> jwtSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tokenGenerator = tokenGenerator;
            _jwtSettings = jwtSettings.Value;
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
            var response = new AuthResponseDto();

            // Validate unique username and email
            var isUsernameUnique = await _unitOfWork.Users.IsUsernameUniqueAsync(registerDto.Username);
            if (!isUsernameUnique)
            {
                response.IsSuccess = false;
                response.Errors.Add("Username is already taken");
            }

            var isEmailUnique = await _unitOfWork.Users.IsEmailUniqueAsync(registerDto.Email);
            if (!isEmailUnique)
            {
                response.IsSuccess = false;
                response.Errors.Add("Email is already registered");
            }

            if (!response.IsSuccess)
            {
                return response;
            }

            // Map and create user
            var user = _mapper.Map<User>(registerDto);
            
            // Hash password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
            
            // Add default role
            user.Roles.Add("User");

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            // Generate tokens
            var token = _tokenGenerator.GenerateJwtToken(user);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();

            // Save refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CompleteAsync();

            response.IsSuccess = true;
            response.Message = "Registration successful";
            response.Token = token;
            response.RefreshToken = refreshToken;
            response.Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
            
            return response;
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
    }
} 
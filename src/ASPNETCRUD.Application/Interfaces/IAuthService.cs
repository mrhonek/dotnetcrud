using ASPNETCRUD.Application.DTOs;

namespace ASPNETCRUD.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string username);
        
        // Diagnostic methods
        DatabaseTestResult TestDatabaseConnection();
        bool IsJwtConfigured();
    }
    
    public class DatabaseTestResult
    {
        public int UserCount { get; set; }
        public string DatabaseName { get; set; } = string.Empty; // Initialize with empty string to fix nullable warning
    }
} 
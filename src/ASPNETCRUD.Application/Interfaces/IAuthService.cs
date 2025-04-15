using ASPNETCRUD.Application.DTOs;

namespace ASPNETCRUD.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string username);
        
        // Diagnostic method
        DatabaseTestResult TestDatabaseConnection();
    }
    
    public class DatabaseTestResult
    {
        public int UserCount { get; set; }
        public string DatabaseName { get; set; }
    }
} 
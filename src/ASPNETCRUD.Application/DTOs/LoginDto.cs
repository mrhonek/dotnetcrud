namespace ASPNETCRUD.Application.DTOs
{
    public class LoginDto
    {
        // This field can contain either username or email
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
} 
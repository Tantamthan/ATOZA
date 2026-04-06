using System.ComponentModel.DataAnnotations;

namespace ATOZA.Application.DTOs
{
    public class LoginDto
    {
        [Required] public string UserName { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string UserName { get; set; } = string.Empty;
        [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
        [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // dùng cho RememberMe
    }
}

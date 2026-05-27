using ATOZA.Application.DTOs;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IAuthService
    {
        Task<UserProfileDto?> LoginAsync(string username, string password);
        Task<UserProfileDto?> LoginWithGoogleAsync(string email);
        Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string? Error, UserProfileDto? Profile)> RegisterWithGoogleAsync(RegisterDto dto, string googleEmail);
        Task<PasswordResetTokenDto?> CreatePasswordResetTokenAsync(string email);
        Task<(bool Success, string? Error, string? Email, string? FullName)> ResetPasswordAsync(ResetPasswordDto dto);
        Task<bool> IsEmailRegisteredAsync(string email);
        Task<bool> IsEmailOrUsernameTakenAsync(string email, string username);
    }
}

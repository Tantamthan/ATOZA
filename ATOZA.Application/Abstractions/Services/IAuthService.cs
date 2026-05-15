using ATOZA.Application.DTOs;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IAuthService
    {
        Task<UserProfileDto?> LoginAsync(string username, string password);
        Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto);
        Task<bool> IsEmailOrUsernameTakenAsync(string email, string username);

    }
}

using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace ATOZA.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _db;

        public AuthService(IApplicationDbContext db) => _db = db;

        public async Task<UserProfileDto?> LoginAsync(string username, string password)
        {
            string hashed = GetMD5(password);
            var user = await Task.FromResult(
                _db.Users.FirstOrDefault(u =>
                    u.UserName == username && u.PasswordHash == hashed));

            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role.ToString(),
                PasswordHash = user.PasswordHash
            };
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto)
        {
            if (await IsEmailOrUsernameTakenAsync(dto.Email, dto.UserName))
                return (false, "Email hoặc Tên đăng nhập đã tồn tại");

            var role = Enum.TryParse<UserRole>(dto.Role, out var parsedRole)
                ? parsedRole : UserRole.Student;

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.UserName,
                PasswordHash = GetMD5(dto.Password),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public Task<bool> IsEmailOrUsernameTakenAsync(string email, string username)
        {
            return Task.FromResult(
                _db.Users.Any(u => u.Email == email || u.UserName == username));
        }

        private static string GetMD5(string str)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(str);
            var hash = md5.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}

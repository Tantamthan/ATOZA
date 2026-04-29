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
            var user = await Task.FromResult(
                _db.Users.FirstOrDefault(u => u.UserName == username));

            if (user == null || !user.IsActive || !VerifyPassword(password, user.PasswordHash))
                return null;

            if (IsLegacyMd5Hash(user.PasswordHash))
            {
                user.PasswordHash = HashPassword(password);
                await _db.SaveChangesAsync();
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role.ToString()
            };
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto)
        {
            if (await IsEmailOrUsernameTakenAsync(dto.Email, dto.UserName))
                return (false, "Email hoặc Tên đăng nhập đã tồn tại");

            var role = Enum.TryParse<UserRole>(dto.Role, out var parsedRole)
                ? parsedRole : UserRole.Student;
            if (role == UserRole.Admin)
                return (false, "Khong the dang ky tai khoan Admin.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.UserName,
                PasswordHash = HashPassword(dto.Password),
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

        private static string HashPassword(string password)
        {
            const int iterationCount = 100_000;
            const int saltSize = 16;
            const int keySize = 32;

            var salt = RandomNumberGenerator.GetBytes(saltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterationCount,
                HashAlgorithmName.SHA256,
                keySize);

            return $"PBKDF2${iterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            if (IsLegacyMd5Hash(storedHash))
                return FixedTimeEquals(GetMD5(password), storedHash);

            var parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2")
                return false;

            if (!int.TryParse(parts[1], out var iterationCount))
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);
                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterationCount,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsLegacyMd5Hash(string hash) =>
            hash.Length == 32 && hash.All(Uri.IsHexDigit);

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
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

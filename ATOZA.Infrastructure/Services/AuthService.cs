using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
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
            var normalizedLogin = username.Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.UserName == normalizedLogin || u.Email == normalizedLogin);

            if (user == null || !user.IsActive || !IsApprovedForLogin(user) || !VerifyPassword(password, user.PasswordHash))
                return null;

            if (IsLegacyMd5Hash(user.PasswordHash))
            {
                user.PasswordHash = HashPassword(password);
                await _db.SaveChangesAsync();
            }

            return ToProfile(user);
        }

        public async Task<UserProfileDto?> LoginWithGoogleAsync(string email)
        {
            var normalizedEmail = email.Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !user.IsActive || !IsApprovedForLogin(user))
                return null;

            return ToProfile(user);
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(RegisterDto dto)
        {
            var passwordError = ValidatePassword(dto.Password, dto.ConfirmPassword);
            if (passwordError != null)
                return (false, passwordError);

            if (await IsEmailOrUsernameTakenAsync(dto.Email, dto.UserName))
                return (false, "Email hoặc Tên đăng nhập đã tồn tại");

            var role = Enum.TryParse<UserRole>(dto.Role, out var parsedRole)
                ? parsedRole : UserRole.Student;
            if (role == UserRole.Admin)
                return (false, "Không thể đăng ký tài khoản Admin.");

            var approvalStatus = role == UserRole.Teacher
                ? ApprovalStatus.Pending
                : ApprovalStatus.Approved;

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.UserName,
                PasswordHash = HashPassword(dto.Password),
                Role = role,
                IsActive = approvalStatus == ApprovalStatus.Approved,
                ApprovalStatus = approvalStatus,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error, UserProfileDto? Profile)> RegisterWithGoogleAsync(
            RegisterDto dto,
            string googleEmail)
        {
            var normalizedGoogleEmail = googleEmail.Trim();
            if (string.IsNullOrWhiteSpace(normalizedGoogleEmail) ||
                !string.Equals(dto.Email.Trim(), normalizedGoogleEmail, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Email Google không hợp lệ.", null);
            }

            if (await _db.Users.AnyAsync(u => u.Email == normalizedGoogleEmail))
                return (false, "Email đã tồn tại", null);

            var userName = dto.UserName.Trim();
            if (string.IsNullOrWhiteSpace(userName))
                return (false, "Vui lòng nhập tên đăng nhập.", null);

            if (await _db.Users.AnyAsync(u => u.UserName == userName))
                return (false, "Tên đăng nhập đã tồn tại", null);

            var role = Enum.TryParse<UserRole>(dto.Role, out var parsedRole)
                ? parsedRole : UserRole.Student;
            if (role == UserRole.Admin)
                return (false, "Không thể đăng ký tài khoản Admin.", null);

            var approvalStatus = role == UserRole.Teacher
                ? ApprovalStatus.Pending
                : ApprovalStatus.Approved;

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = normalizedGoogleEmail,
                UserName = userName,
                PasswordHash = CreateExternalPasswordHash("Google"),
                Role = role,
                IsActive = approvalStatus == ApprovalStatus.Approved,
                ApprovalStatus = approvalStatus,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var profile = user.IsActive && IsApprovedForLogin(user)
                ? ToProfile(user)
                : null;

            return (true, null, profile);
        }

        public Task<bool> IsEmailRegisteredAsync(string email)
        {
            var normalizedEmail = email.Trim();
            return _db.Users.AnyAsync(u => u.Email == normalizedEmail);
        }

        public Task<bool> IsEmailOrUsernameTakenAsync(string email, string username)
        {
            return _db.Users.AnyAsync(u => u.Email == email || u.UserName == username);
        }

        public async Task<PasswordResetRequestResultDto> RequestPasswordResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new PasswordResetRequestResultDto();

            var normalizedEmail = email.Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || !user.IsActive || !IsApprovedForLogin(user))
                return new PasswordResetRequestResultDto { EmailExists = false };

            var now = DateTime.UtcNow;
            var existingTokens = await _db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAtUtc == null)
                .ToListAsync();

            foreach (var existingToken in existingTokens)
                existingToken.UsedAtUtc = now;

            var token = CreateResetToken();
            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = HashResetToken(token),
                ExpiresAtUtc = now.AddMinutes(30),
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            return new PasswordResetRequestResultDto
            {
                EmailExists = true,
                ResetToken = token
            };
        }

        public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var passwordError = ValidatePassword(dto.Password, dto.ConfirmPassword);
            if (passwordError != null)
                return (false, passwordError);

            if (string.IsNullOrWhiteSpace(dto.Token))
                return (false, "Liên kết đặt lại mật khẩu không hợp lệ.");

            var tokenHash = HashResetToken(dto.Token.Trim());
            var now = DateTime.UtcNow;
            var resetToken = await _db.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAtUtc == null);

            if (resetToken == null || resetToken.ExpiresAtUtc <= now)
                return (false, "Liên kết đặt lại mật khẩu đã hết hạn hoặc không hợp lệ.");

            if (!resetToken.User.IsActive || !IsApprovedForLogin(resetToken.User))
                return (false, "Tài khoản chưa được kích hoạt hoặc chưa được duyệt.");

            resetToken.User.PasswordHash = HashPassword(dto.Password);
            resetToken.UsedAtUtc = now;

            await _db.SaveChangesAsync();
            return (true, null);
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

        private static string CreateExternalPasswordHash(string provider) =>
            $"EXTERNAL:{provider}:{Guid.NewGuid():N}";

        private static string CreateResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashResetToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private static string? ValidatePassword(string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Vui lòng nhập mật khẩu.";

            if (password.Length < 6)
                return "Mật khẩu phải có ít nhất 6 ký tự.";

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
                return "Mật khẩu xác nhận không khớp.";

            return null;
        }

        private static UserProfileDto ToProfile(User user) =>
            new()
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role.ToString()
            };

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

        private static bool IsApprovedForLogin(User user) =>
            user.Role != UserRole.Teacher || user.ApprovalStatus == ApprovalStatus.Approved;

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

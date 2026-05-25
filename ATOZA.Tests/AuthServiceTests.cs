using System.Security.Cryptography;
using System.Text;
using ATOZA.Application.DTOs;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using ATOZA.Infrastructure.Persistence;
using ATOZA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WithLegacyMd5Password_ReturnsProfileAndUpgradesHash()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 10,
            FullName = "Test Teacher",
            Email = "teacher@example.com",
            UserName = "teacher",
            PasswordHash = Md5("secret"),
            Role = UserRole.Teacher,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        var profile = await service.LoginAsync("teacher", "secret");

        Assert.NotNull(profile);
        Assert.Equal("teacher", profile.UserName);
        Assert.StartsWith("PBKDF2$", db.Users.Single(u => u.UserName == "teacher").PasswordHash);
    }

    [Fact]
    public async Task IsEmailOrUsernameTakenAsync_UsesPersistedUsers()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 11,
            FullName = "Existing Student",
            Email = "student@example.com",
            UserName = "student",
            PasswordHash = Md5("secret"),
            Role = UserRole.Student,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        Assert.True(await service.IsEmailOrUsernameTakenAsync("student@example.com", "other"));
        Assert.True(await service.IsEmailOrUsernameTakenAsync("other@example.com", "student"));
        Assert.False(await service.IsEmailOrUsernameTakenAsync("new@example.com", "newuser"));
    }

    [Fact]
    public async Task LoginAsync_WithEmail_ReturnsProfile()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 13,
            FullName = "Email Login Student",
            Email = "email-login@example.com",
            UserName = "emaillogin",
            PasswordHash = Md5("secret"),
            Role = UserRole.Student,
            IsActive = true,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        var profile = await service.LoginAsync("email-login@example.com", "secret");

        Assert.NotNull(profile);
        Assert.Equal("emaillogin", profile.UserName);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WithExistingActiveUser_ReturnsProfile()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 12,
            FullName = "Google Student",
            Email = "google@example.com",
            UserName = "googlestudent",
            PasswordHash = Md5("secret"),
            Role = UserRole.Student,
            IsActive = true,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        var profile = await service.LoginWithGoogleAsync("google@example.com");

        Assert.NotNull(profile);
        Assert.Equal("googlestudent", profile.UserName);
    }

    [Fact]
    public async Task RegisterWithGoogleAsync_NewStudent_CreatesActiveExternalAccount()
    {
        await using var db = CreateDbContext();
        var service = new AuthService(db);

        var result = await service.RegisterWithGoogleAsync(new RegisterDto
        {
            FullName = "New Google Student",
            Email = "newgoogle@example.com",
            UserName = "newgoogle",
            Role = "Student",
            IsExternalLogin = true
        }, "newgoogle@example.com");

        Assert.True(result.Success);
        Assert.NotNull(result.Profile);

        var user = await db.Users.SingleAsync(u => u.Email == "newgoogle@example.com");
        Assert.True(user.IsActive);
        Assert.Equal(ApprovalStatus.Approved, user.ApprovalStatus);
        Assert.StartsWith("EXTERNAL:Google:", user.PasswordHash);
        Assert.Null(await service.LoginAsync("newgoogle", "anything"));
    }

    [Fact]
    public async Task RegisterWithGoogleAsync_NewTeacherStartsPending()
    {
        await using var db = CreateDbContext();
        var service = new AuthService(db);

        var result = await service.RegisterWithGoogleAsync(new RegisterDto
        {
            FullName = "New Google Teacher",
            Email = "googleteacher@example.com",
            UserName = "googleteacher",
            Role = "Teacher",
            IsExternalLogin = true
        }, "googleteacher@example.com");

        Assert.True(result.Success);
        Assert.Null(result.Profile);

        var user = await db.Users.SingleAsync(u => u.Email == "googleteacher@example.com");
        Assert.False(user.IsActive);
        Assert.Equal(ApprovalStatus.Pending, user.ApprovalStatus);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithExistingActiveUser_CreatesToken()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 20,
            FullName = "Reset Student",
            Email = "reset@example.com",
            UserName = "resetstudent",
            PasswordHash = Md5("oldsecret"),
            Role = UserRole.Student,
            IsActive = true,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        var result = await service.RequestPasswordResetAsync("reset@example.com");

        Assert.True(result.EmailExists);
        Assert.False(string.IsNullOrWhiteSpace(result.ResetToken));

        var token = await db.PasswordResetTokens.SingleAsync();
        Assert.Equal(20, token.UserId);
        Assert.Null(token.UsedAtUtc);
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithInactiveUser_DoesNotCreateToken()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 21,
            FullName = "Inactive Student",
            Email = "inactive@example.com",
            UserName = "inactive",
            PasswordHash = Md5("oldsecret"),
            Role = UserRole.Student,
            IsActive = false,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);

        var result = await service.RequestPasswordResetAsync("inactive@example.com");

        Assert.False(result.EmailExists);
        Assert.Null(result.ResetToken);
        Assert.False(await db.PasswordResetTokens.AnyAsync());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ChangesPasswordAndConsumesToken()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 22,
            FullName = "Reset Login Student",
            Email = "reset-login@example.com",
            UserName = "resetlogin",
            PasswordHash = Md5("oldsecret"),
            Role = UserRole.Student,
            IsActive = true,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);
        var request = await service.RequestPasswordResetAsync("reset-login@example.com");

        var result = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            Token = request.ResetToken!,
            Password = "newsecret",
            ConfirmPassword = "newsecret"
        });

        Assert.True(result.Success);
        Assert.NotNull((await db.PasswordResetTokens.SingleAsync()).UsedAtUtc);
        Assert.Null(await service.LoginAsync("resetlogin", "oldsecret"));
        Assert.NotNull(await service.LoginAsync("resetlogin", "newsecret"));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ReturnsError()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = 23,
            FullName = "Expired Reset Student",
            Email = "expired-reset@example.com",
            UserName = "expiredreset",
            PasswordHash = Md5("oldsecret"),
            Role = UserRole.Student,
            IsActive = true,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AuthService(db);
        var request = await service.RequestPasswordResetAsync("expired-reset@example.com");
        var token = await db.PasswordResetTokens.SingleAsync();
        token.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var result = await service.ResetPasswordAsync(new ResetPasswordDto
        {
            Token = request.ResetToken!,
            Password = "newsecret",
            ConfirmPassword = "newsecret"
        });

        Assert.False(result.Success);
        Assert.Null(await service.LoginAsync("expiredreset", "newsecret"));
    }

    private static ATOZADbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ATOZADbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ATOZADbContext(options);
    }

    private static string Md5(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = MD5.HashData(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}

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

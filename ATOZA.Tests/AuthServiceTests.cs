using System.Security.Cryptography;
using System.Text;
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

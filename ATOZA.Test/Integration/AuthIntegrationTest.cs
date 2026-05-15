using Xunit;
using FluentAssertions;
using ATOZA.Infrastructure.Services;
using ATOZA.Application.DTOs;
using ATOZA.Test.Helpers;

namespace ATOZA.Test.Integration;

public class AuthIntegrationTest
{
    [Fact]
    public async Task Register_Then_Login_Success()
    {
        // Arrange
        var db = TestDbContextFactory.Create();

        var service = new AuthService(db);

        var registerDto = new RegisterDto
        {
            FullName = "Hung",
            Email = "hung@gmail.com",
            UserName = "hung",
            Password = "123456",
            Role = "Student"
        };

        // Act
        await service.RegisterAsync(registerDto);

        var result = await service.LoginAsync(
            "hung",
            "123456");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("hung");
    }
}
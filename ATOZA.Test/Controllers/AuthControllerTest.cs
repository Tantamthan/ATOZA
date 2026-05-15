using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs;
using Atoza_Web.Controllers;

namespace ATOZA.Test.Controllers;

public class AuthControllerTest
{
    [Fact]
    public async Task Register_ReturnOk()
    {
        // --- 1. Arrange: Thiết lập môi trường giả lập ---
        var mockService = new Mock<IAuthService>();

        // Mock Logger để tránh lỗi NullReferenceException nếu Controller có dùng log
        var mockLogger = new Mock<ILogger<AccountController>>();

        // Giả lập RegisterAsync luôn trả về thành công (true)
        mockService.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                   .ReturnsAsync((true, (string?)null));

        // Khởi tạo Controller với các tham số Mock
    
        var controller = new AccountController(mockService.Object);

        var dto = new RegisterDto
        {
            UserName = "hung",
            Email = "hung@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // --- 2. Act: Thực thi hành động ---
        var result = await controller.Register(dto);

        // --- 3. Assert: Kiểm tra kết quả ---

     
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;

        // Kiểm tra xem nó có chuyển hướng về đúng Action "Login" không (hoặc tên Action bạn đã đặt)
        redirectResult.ActionName.Should().Be("Login");

        // Kiểm tra xem kết quả trả về có khác null không
        result.Should().NotBeNull();
    }
}
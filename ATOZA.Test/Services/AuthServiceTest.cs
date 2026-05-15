using Xunit;
using FluentAssertions;

using ATOZA.Infrastructure.Services;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using ATOZA.Test.Helpers;
using ATOZA.Application.DTOs;

namespace ATOZA.Test.Services;

public class AuthServiceTest
{
  
   
    // Kiểm tra đăng nhập với username không tồn tại
    // Kết quả mong muốn: trả về null
 
    [Fact]
    public async Task LoginAsync_WithWrongUsername_ReturnNull()
    {
     
        // Tạo database giả trong bộ nhớ
        var db = TestDbContextFactory.Create();

        // Tạo service cần test
        var service = new AuthService(db);

       
        // Thử đăng nhập với username không tồn tại
        var result = await service.LoginAsync(
            "wronguser",
            "123");

       
        // Kiểm tra kết quả phải là null
        result.Should().BeNull();
    }

    
    // Kiểm tra đăng nhập với tài khoản bị khóa
    // Kết quả mong muốn: trả về null

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnNull()
    {
  
        // Tạo database giả
        var db = TestDbContextFactory.Create();

        // Thêm user bị khóa vào database
        db.Users.Add(new User
        {
            FullName = "Hung",
            UserName = "hung",
            Email = "hung@gmail.com",

            // Password giả
            PasswordHash = "123",

            Role = UserRole.Student,

            // Tài khoản bị khóa
            IsActive = false
        });

        // Lưu dữ liệu
        await db.SaveChangesAsync();

        // Tạo service
        var service = new AuthService(db);

    
        // Thử đăng nhập
        var result = await service.LoginAsync(
            "hung",
            "123");

        
        // Kết quả phải là null
        result.Should().BeNull();
    }

  
    // Kiểm tra đăng ký tài khoản Admin
    // Hệ thống không cho phép tạo Admin
    // Kết quả mong muốn: Success = false
 
    [Fact]
    public async Task RegisterAsync_WithAdminRole_ReturnFalse()
    {
        // ---------- Arrange ----------
        // Tạo database giả
        var db = TestDbContextFactory.Create();

        // Tạo service
        var service = new AuthService(db);

        // Dữ liệu đăng ký
        var dto = new RegisterDto
        {
            FullName = "Admin",
            Email = "admin@gmail.com",
            UserName = "admin",
            Password = "123",
            Role = "Admin"
        };

        // ---------- Act ----------
        // Gọi hàm đăng ký
        var result = await service.RegisterAsync(dto);

        // ---------- Assert ----------
        // Kiểm tra đăng ký thất bại
        result.Success.Should().BeFalse();
    }
}
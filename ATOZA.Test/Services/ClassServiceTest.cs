using Xunit;
using FluentAssertions;
using ATOZA.Infrastructure.Services;
using ATOZA.Domain.Entities;
using ATOZA.Test.Helpers;
using ATOZA.Application.DTOs.Class;

namespace ATOZA.Test.Services;

public class ClassServiceTest
{
    [Fact]
    public async Task AssignExamAsync_WithInvalidDate_ReturnError()
    {
        // Arrange
        var db = TestDbContextFactory.Create();

        var service = new ClassService(db);

        var dto = new AssignExamDto
        {
            ClassId = 1,
            ExamId = 1,
            AvailableFrom = DateTime.Now,
            DueDate = DateTime.Now.AddHours(-1)
        };

        // Act
        var result = await service.AssignExamAsync(dto, 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AssignExamAsync_WithUnauthorizedTeacher_ReturnError()
    {
        // Arrange
        var db = TestDbContextFactory.Create();

        db.Classes.Add(new Class
        {
            Id = 1,
            TeacherId = 99
        });

        await db.SaveChangesAsync();

        var service = new ClassService(db);

        var dto = new AssignExamDto
        {
            ClassId = 1,
            ExamId = 1,
            AvailableFrom = DateTime.Now,
            DueDate = DateTime.Now.AddDays(1)
        };

        // Act
        var result = await service.AssignExamAsync(dto, 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AssignExamAsync_WithValidData_ReturnSuccess()
    {
        // Arrange
        var db = TestDbContextFactory.Create();

        db.Classes.Add(new Class
        {
            Id = 1,
            TeacherId = 1
        });

        db.Exams.Add(new Exam
        {
            Id = 1,
            CreatorId = 1,
            IsPublic = false
        });

        await db.SaveChangesAsync();

        var service = new ClassService(db);

        var dto = new AssignExamDto
        {
            ClassId = 1,
            ExamId = 1,
            AvailableFrom = DateTime.Now,
            DueDate = DateTime.Now.AddDays(1)
        };

        // Act
        var result = await service.AssignExamAsync(dto, 1);

        // Assert
        result.Success.Should().BeTrue();
    }
}
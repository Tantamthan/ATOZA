using ATOZA.Application.DTOs.Exam;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Exceptions;
using ATOZA.Infrastructure.Persistence;
using ATOZA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Tests;

public class ExamServiceTests
{
    [Fact]
    public async Task UpdateExamAsync_WhenExamDoesNotExist_ThrowsNotFoundException()
    {
        await using var db = CreateDbContext();
        var service = new ExamService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateExamAsync(new UpdateExamDto { ExamId = 404 }, teacherId: 1));
    }

    [Fact]
    public async Task UpdateExamAsync_WhenTeacherDoesNotOwnExam_ThrowsUnauthorizedException()
    {
        await using var db = CreateDbContext();
        db.Exams.Add(new Exam
        {
            Id = 20,
            Title = "Owned by another teacher",
            CreatorId = 1,
            DurationMinutes = 30,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ExamService(db);

        await Assert.ThrowsAsync<ATOZA.Domain.Exceptions.UnauthorizedException>(() =>
            service.UpdateExamAsync(new UpdateExamDto { ExamId = 20 }, teacherId: 2));
    }

    private static ATOZADbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ATOZADbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ATOZADbContext(options);
    }
}

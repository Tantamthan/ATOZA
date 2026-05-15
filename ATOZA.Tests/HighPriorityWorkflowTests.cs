using ATOZA.Application.DTOs;
using ATOZA.Application.DTOs.Exam;
using ATOZA.Application.DTOs.Submission;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using ATOZA.Infrastructure.Persistence;
using ATOZA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Tests;

public class HighPriorityWorkflowTests
{
    [Fact]
    public async Task RegisterAsync_TeacherStartsPendingAndCannotLoginUntilApproved()
    {
        await using var db = CreateDbContext();
        var authService = new AuthService(db);
        var adminService = new AdminService(db);

        var register = await authService.RegisterAsync(new RegisterDto
        {
            FullName = "Pending Teacher",
            Email = "pending@example.com",
            UserName = "pendingteacher",
            Password = "secret123",
            ConfirmPassword = "secret123",
            Role = "Teacher"
        });

        Assert.True(register.Success);
        var teacher = await db.Users.SingleAsync(u => u.UserName == "pendingteacher");
        Assert.Equal(ApprovalStatus.Pending, teacher.ApprovalStatus);
        Assert.False(teacher.IsActive);
        Assert.Null(await authService.LoginAsync("pendingteacher", "secret123"));

        Assert.True(await adminService.SetTeacherApprovalStatusAsync(teacher.Id, ApprovalStatus.Approved));
        Assert.NotNull(await authService.LoginAsync("pendingteacher", "secret123"));
    }

    [Fact]
    public async Task UpdateExamAsync_WhenExamHasAssignment_CreatesNewVersionAndKeepsOldAssignment()
    {
        await using var db = CreateDbContext();
        SeedAssignedExam(db);
        await db.SaveChangesAsync();

        var service = new ExamService(db);

        var result = await service.UpdateExamAsync(new UpdateExamDto
        {
            ExamId = 100,
            Title = "Updated Exam",
            DurationMinutes = 45,
            ExamMode = "Assessment",
            Questions =
            {
                NewQuestionDto(1, "New question", "B")
            }
        }, teacherId: 1);

        Assert.True(result.CreatedNewVersion);
        Assert.NotEqual(100, result.ExamId);

        var oldExam = await db.Exams.Include(e => e.Questions).SingleAsync(e => e.Id == 100);
        var newExam = await db.Exams.Include(e => e.Questions).SingleAsync(e => e.Id == result.ExamId);
        var assignment = await db.ClassAssignments.SingleAsync(a => a.Id == 300);

        Assert.True(oldExam.IsArchived);
        Assert.Equal(100, assignment.ExamId);
        Assert.Equal("Original question", oldExam.Questions.Single().Content);
        Assert.Equal(100, newExam.ParentExamId);
        Assert.Equal(2, newExam.VersionNumber);
        Assert.Equal("New question", newExam.Questions.Single().Content);
    }

    [Fact]
    public async Task StartAttemptAsync_RefreshReturnsSameActiveAttempt()
    {
        await using var db = CreateDbContext();
        SeedAssignedExam(db);
        await db.SaveChangesAsync();

        var service = new ExamAttemptService(db);

        var first = await service.StartAttemptAsync(100, studentId: 2);
        var second = await service.StartAttemptAsync(100, studentId: 2);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.AttemptId, second.AttemptId);
        Assert.True(first.ExpiresAtUtc > first.ServerNowUtc);
    }

    [Fact]
    public async Task SubmitExamAsync_WhenAttemptExpired_ReturnsFriendlyErrorAndDoesNotCreateSubmission()
    {
        await using var db = CreateDbContext();
        SeedAssignedExam(db);
        db.ExamAttempts.Add(new ExamAttempt
        {
            Id = 700,
            ExamId = 100,
            StudentId = 2,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-60),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-30),
            Status = AttemptStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddMinutes(-60)
        });
        await db.SaveChangesAsync();

        var service = new SubmissionService(db);

        var result = await service.SubmitExamAsync(new SubmitExamDto
        {
            ExamId = 100,
            AttemptId = 700,
            Answers =
            {
                new StudentAnswerDto { QuestionId = 200, SelectedOption = "A" }
            }
        }, studentId: 2);

        Assert.False(result.Success);
        Assert.Contains("het thoi gian", result.Message);
        Assert.False(await db.Submissions.AnyAsync());
        Assert.Equal(AttemptStatus.Expired, (await db.ExamAttempts.SingleAsync(a => a.Id == 700)).Status);
    }

    [Fact]
    public void QuestionCard_DoesNotRenderQuestionContentWithHtmlRaw()
    {
        var viewPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../Atoza/Views/exam/QuestionCard.cshtml"));

        var viewContent = File.ReadAllText(viewPath);

        Assert.DoesNotContain("@Html.Raw(Model.Content)", viewContent);
        Assert.Contains("@Model.Content", viewContent);
    }

    private static ATOZADbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ATOZADbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ATOZADbContext(options);
    }

    private static void SeedAssignedExam(ATOZADbContext db)
    {
        var now = DateTime.UtcNow;
        db.Users.AddRange(
            new User
            {
                Id = 1,
                FullName = "Teacher",
                Email = "teacher@example.com",
                UserName = "teacher",
                PasswordHash = "hash",
                Role = UserRole.Teacher,
                ApprovalStatus = ApprovalStatus.Approved,
                IsActive = true
            },
            new User
            {
                Id = 2,
                FullName = "Student",
                Email = "student@example.com",
                UserName = "student",
                PasswordHash = "hash",
                Role = UserRole.Student,
                ApprovalStatus = ApprovalStatus.Approved,
                IsActive = true
            });

        db.Exams.Add(new Exam
        {
            Id = 100,
            Title = "Original Exam",
            CreatorId = 1,
            DurationMinutes = 30,
            VersionNumber = 1,
            StartTime = now,
            EndTime = now.AddMinutes(30),
            CreatedAt = now
        });

        db.Questions.Add(new Question
        {
            Id = 200,
            ExamId = 100,
            OrderNumber = 1,
            Content = "Original question",
            OptionA = "A",
            OptionB = "B",
            OptionC = "C",
            OptionD = "D",
            CorrectAnswer = "A"
        });

        db.Classes.Add(new Class
        {
            Id = 400,
            ClassName = "Class 1",
            TeacherId = 1,
            JoinCode = "ABC123",
            CreatedAt = now
        });

        db.ClassStudents.Add(new ClassStudent
        {
            ClassId = 400,
            StudentId = 2,
            JoinedAt = now
        });

        db.ClassAssignments.Add(new ClassAssignment
        {
            Id = 300,
            ClassId = 400,
            ExamId = 100,
            AvailableFrom = now.AddMinutes(-5),
            DueDate = now.AddHours(1),
            AssignedAt = now.AddMinutes(-10),
            CreatedAt = now.AddMinutes(-10)
        });
    }

    private static QuestionDto NewQuestionDto(int orderNumber, string content, string correctAnswer) =>
        new()
        {
            OrderNumber = orderNumber,
            Content = content,
            OptionA = "A",
            OptionB = "B",
            OptionC = "C",
            OptionD = "D",
            CorrectAnswer = correctAnswer
        };
}

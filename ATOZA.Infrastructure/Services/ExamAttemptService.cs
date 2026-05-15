using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Exam;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Infrastructure.Services
{
    public class ExamAttemptService : IExamAttemptService
    {
        private readonly IApplicationDbContext _db;

        public ExamAttemptService(IApplicationDbContext db) => _db = db;

        public async Task<ExamAttemptResultDto> StartAttemptAsync(int examId, int studentId)
        {
            var now = DateTime.UtcNow;

            if (await _db.Submissions.AnyAsync(s => s.ExamId == examId && s.StudentId == studentId))
                return Fail("Ban da nop bai thi nay roi.", now);

            var assignment = await GetOpenAssignmentForStudentAsync(examId, studentId, now);
            if (assignment == null)
                return Fail("Ban khong co quyen lam bai thi nay hoac bai thi khong trong thoi gian mo.", now);

            var existingAttempt = await _db.ExamAttempts
                .Where(a => a.ExamId == examId && a.StudentId == studentId && a.Status == AttemptStatus.InProgress)
                .OrderByDescending(a => a.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (existingAttempt != null)
            {
                if (existingAttempt.ExpiresAtUtc <= now)
                {
                    existingAttempt.Status = AttemptStatus.Expired;
                    await _db.SaveChangesAsync();
                    return Fail("Luot lam bai da het thoi gian.", now);
                }

                return Success(existingAttempt, now);
            }

            var expiresAt = now.AddMinutes(assignment.Exam.DurationMinutes);
            if (expiresAt > assignment.DueDate)
                expiresAt = assignment.DueDate;

            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = studentId,
                StartedAtUtc = now,
                ExpiresAtUtc = expiresAt,
                Status = AttemptStatus.InProgress,
                CreatedAt = now
            };

            _db.ExamAttempts.Add(attempt);
            await _db.SaveChangesAsync();

            return Success(attempt, now);
        }

        public async Task<ExamAttemptResultDto> GetActiveAttemptAsync(int examId, int studentId)
        {
            var now = DateTime.UtcNow;
            var attempt = await _db.ExamAttempts
                .Where(a => a.ExamId == examId && a.StudentId == studentId && a.Status == AttemptStatus.InProgress)
                .OrderByDescending(a => a.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (attempt == null)
                return Fail("Chua co luot lam bai dang mo.", now);

            if (attempt.ExpiresAtUtc <= now)
            {
                attempt.Status = AttemptStatus.Expired;
                await _db.SaveChangesAsync();
                return Fail("Luot lam bai da het thoi gian.", now);
            }

            return Success(attempt, now);
        }

        private Task<ClassAssignment?> GetOpenAssignmentForStudentAsync(int examId, int studentId, DateTime now)
        {
            return _db.ClassAssignments
                .Include(a => a.Exam)
                .Where(a => a.ExamId == examId)
                .Where(a => a.AvailableFrom <= now && a.DueDate >= now)
                .Where(a => a.Class.ClassStudents.Any(cs => cs.StudentId == studentId))
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync();
        }

        private static ExamAttemptResultDto Success(ExamAttempt attempt, DateTime now) =>
            new()
            {
                Success = true,
                AttemptId = attempt.Id,
                StartedAtUtc = attempt.StartedAtUtc,
                ExpiresAtUtc = attempt.ExpiresAtUtc,
                ServerNowUtc = now
            };

        private static ExamAttemptResultDto Fail(string message, DateTime now) =>
            new()
            {
                Success = false,
                Message = message,
                ServerNowUtc = now
            };
    }
}

using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATOZA.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IApplicationDbContext _db;

        public AdminService(IApplicationDbContext db) => _db = db;

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            return new DashboardStatsDto
            {
                TotalTeachers = await _db.Users.CountAsync(u => u.Role == UserRole.Teacher),
                TotalStudents = await _db.Users.CountAsync(u => u.Role == UserRole.Student),
                TotalExams = await _db.Exams.CountAsync(),
                TotalClasses = await _db.Classes.CountAsync(),
                TotalSubmissions = await _db.Submissions.CountAsync(),
                ActiveUsers = await _db.Users.CountAsync(u => u.IsActive),
                InactiveUsers = await _db.Users.CountAsync(u => !u.IsActive)
            };
        }

        public async Task<List<UserListDto>> GetAllUsersAsync(UserRole? roleFilter = null)
        {
            var query = _db.Users.AsQueryable();
            if (roleFilter.HasValue)
                query = query.Where(u => u.Role == roleFilter.Value);

            return await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> SetUserActiveStatusAsync(int userId, bool isActive)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.IsActive = isActive;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<List<Exam>> GetAllExamsWithCreatorAsync()
        {
            return _db.Exams
                .Include(e => e.Creator)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteExamAsync(int examId)
        {
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return false;

            var submissionIds = await _db.Submissions
                .Where(s => s.ExamId == examId)
                .Select(s => s.Id)
                .ToListAsync();

            if (submissionIds.Count > 0)
            {
                var details = await _db.SubmissionDetails
                    .Where(sd => submissionIds.Contains(sd.SubmissionId))
                    .ToListAsync();
                _db.SubmissionDetails.RemoveRange(details);

                var submissions = await _db.Submissions
                    .Where(s => s.ExamId == examId)
                    .ToListAsync();
                _db.Submissions.RemoveRange(submissions);
            }

            var assignments = await _db.ClassAssignments
                .Where(a => a.ExamId == examId)
                .ToListAsync();
            _db.ClassAssignments.RemoveRange(assignments);
            _db.Questions.RemoveRange(exam.Questions);
            _db.Exams.Remove(exam);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetExamPublicStatusAsync(int examId, bool isPublic)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return false;

            exam.IsPublic = isPublic;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task<List<ClassOverviewDto>> GetAllClassesOverviewAsync()
        {
            return _db.Classes
                .Include(c => c.Teacher)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClassOverviewDto
                {
                    ClassId = c.Id,
                    ClassName = c.ClassName,
                    TeacherName = c.Teacher.FullName,
                    StudentCount = c.ClassStudents.Count,
                    AssignmentCount = c.ClassAssignments.Count,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }
    }
}

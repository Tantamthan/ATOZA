using ATOZA.Application.Abstractions.Persistence;
using ATOZA.Application.Abstractions.Services;
using ATOZA.Application.DTOs.Class;
using ATOZA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ATOZA.Infrastructure.Services
{
    public class ClassService : IClassService
    {
        private readonly IApplicationDbContext _db;

        public ClassService(IApplicationDbContext db) => _db = db;

        public Task<List<Class>> GetClassesByTeacherAsync(int teacherId)
        {
            return _db.Classes
                .Where(c => c.TeacherId == teacherId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Class> CreateClassAsync(CreateClassDto dto, int teacherId)
        {
            var newClass = new Class
            {
                ClassName = dto.ClassName,
                TeacherId = teacherId,
                JoinCode = GenerateRandomCode(6),
                CreatedAt = DateTime.UtcNow
            };

            _db.Classes.Add(newClass);
            await _db.SaveChangesAsync();
            return newClass;
        }

        public Task<Class?> GetClassDetailAsync(int classId, int teacherId)
        {
            return _db.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);
        }

        public async Task<AssignExamResultDto> AssignExamAsync(AssignExamDto dto, int teacherId)
        {
            if (dto.DueDate <= dto.AvailableFrom)
                return AssignExamResult("Han nop phai sau thoi gian bat dau.");

            bool ownsClass = await _db.Classes.AnyAsync(c =>
                c.Id == dto.ClassId && c.TeacherId == teacherId);
            if (!ownsClass)
                return AssignExamResult("Ban khong co quyen giao bai cho lop nay.");

            bool canUseExam = await _db.Exams.AnyAsync(e =>
                e.Id == dto.ExamId && !e.IsArchived && (e.CreatorId == teacherId || e.IsPublic));
            if (!canUseExam)
                return AssignExamResult("Ban khong co quyen su dung de thi nay.");

            bool alreadyAssigned = await _db.ClassAssignments.AnyAsync(a =>
                a.ClassId == dto.ClassId && a.ExamId == dto.ExamId);
            if (alreadyAssigned)
                return AssignExamResult("De thi nay da duoc giao cho lop.");

            _db.ClassAssignments.Add(new ClassAssignment
            {
                ClassId = dto.ClassId,
                ExamId = dto.ExamId,
                AvailableFrom = dto.AvailableFrom,
                DueDate = dto.DueDate,
                AssignedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return new AssignExamResultDto { Success = true };
        }

        public async Task<List<ClassAssignment>> GetClassAssignmentsAsync(int classId, int teacherId)
        {
            bool ownsClass = await _db.Classes.AnyAsync(c =>
                c.Id == classId && c.TeacherId == teacherId);

            if (!ownsClass) return new List<ClassAssignment>();

            return await _db.ClassAssignments
                .Include(a => a.Exam)
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }

        public async Task<byte[]?> ExportStudentsCsvAsync(int classId, int teacherId)
        {
            bool ownsClass = await _db.Classes.AnyAsync(c =>
                c.Id == classId && c.TeacherId == teacherId);

            if (!ownsClass) return null;

            var students = await _db.ClassStudents
                .Include(cs => cs.Student)
                .Where(cs => cs.ClassId == classId)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("STT,Ho va ten,Email,Ngay tham gia");

            int i = 1;
            foreach (var item in students)
            {
                sb.AppendLine($"{i},{item.Student.FullName},{item.Student.Email},{item.JoinedAt:dd/MM/yyyy HH:mm}");
                i++;
            }

            return Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();
        }

        public Task<List<Class>> GetClassesByStudentAsync(int studentId)
        {
            return _db.Classes
                .Include(c => c.Teacher)
                .Where(c => c.ClassStudents.Any(cs => cs.StudentId == studentId))
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> JoinClassAsync(string joinCode, int studentId)
        {
            var targetClass = await _db.Classes.FirstOrDefaultAsync(c => c.JoinCode == joinCode);
            if (targetClass == null)
                return (false, "Ma lop khong ton tai.");

            bool alreadyJoined = await _db.ClassStudents.AnyAsync(cs =>
                cs.ClassId == targetClass.Id && cs.StudentId == studentId);

            if (alreadyJoined)
                return (false, "Ban da tham gia lop nay roi.");

            _db.ClassStudents.Add(new ClassStudent
            {
                ClassId = targetClass.Id,
                StudentId = studentId,
                JoinedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<ClassAssignment>?> GetAssignmentsForStudentAsync(int classId, int studentId)
        {
            bool isInClass = await _db.ClassStudents.AnyAsync(cs =>
                cs.ClassId == classId && cs.StudentId == studentId);

            if (!isInClass) return null;

            return await _db.ClassAssignments
                .Include(a => a.Exam)
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();
        }

        private static AssignExamResultDto AssignExamResult(string error) =>
            new() { Success = false, Error = error };

        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

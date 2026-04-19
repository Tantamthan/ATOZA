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

        // =====================================================
        // GIÁO VIÊN
        // =====================================================

        public Task<List<Class>> GetClassesByTeacherAsync(int teacherId)
        {
            return Task.FromResult(
                _db.Classes
                   .Where(c => c.TeacherId == teacherId)
                   .OrderByDescending(c => c.CreatedAt)
                   .ToList());
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
            return Task.FromResult(
                _db.Classes.FirstOrDefault(c => c.Id == classId && c.TeacherId == teacherId));
        }

        public Task<bool> AssignExamAsync(AssignExamDto dto, out string? errorMessage)
        {
            errorMessage = null;
            if (dto.DueDate <= dto.AvailableFrom)
            {
                errorMessage = "Hạn nộp phải sau thời gian bắt đầu.";
                return Task.FromResult(false);
            }

            _db.ClassAssignments.Add(new ClassAssignment
            {
                ClassId = dto.ClassId,
                ExamId = dto.ExamId,
                AvailableFrom = dto.AvailableFrom,
                DueDate = dto.DueDate,
                AssignedAt = DateTime.UtcNow
            });
            _db.SaveChangesAsync();
            return Task.FromResult(true);
        }

        public Task<List<ClassAssignment>> GetClassAssignmentsAsync(int classId, int teacherId)
        {
            var ownedClass = _db.Classes.FirstOrDefault(c =>
                c.Id == classId && c.TeacherId == teacherId);

            if (ownedClass == null) return Task.FromResult(new List<ClassAssignment>());

            return Task.FromResult(
                _db.ClassAssignments
                   .Where(a => a.ClassId == classId)
                   .OrderByDescending(a => a.AssignedAt)
                   .ToList());
        }

        public Task<byte[]?> ExportStudentsCsvAsync(int classId, int teacherId)
        {
            var targetClass = _db.Classes.FirstOrDefault(c =>
                c.Id == classId && c.TeacherId == teacherId);

            if (targetClass == null) return Task.FromResult<byte[]?>(null);

            var students = _db.ClassStudents
                              .Include(cs => cs.Student)
                              .Where(cs => cs.ClassId == classId)
                              .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("STT,Ho va ten,Email,Ngay tham gia");

            int i = 1;
            foreach (var item in students)
            {
                sb.AppendLine($"{i},{item.Student.FullName},{item.Student.Email}," +
                              $"{item.JoinedAt:dd/MM/yyyy HH:mm}");
                i++;
            }

            // BOM UTF-8 để Excel đọc được tiếng Việt
            var data = Encoding.UTF8.GetPreamble()
                               .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                               .ToArray();

            return Task.FromResult<byte[]?>(data);
        }

        // =====================================================
        // HỌC SINH
        // =====================================================

        public Task<List<Class>> GetClassesByStudentAsync(int studentId)
        {
            return Task.FromResult(
                _db.Classes
                   .Include(c => c.Teacher)
                   .Where(c => c.ClassStudents.Any(cs => cs.StudentId == studentId))
                   .ToList());
        }

        public async Task<(bool Success, string? Error)> JoinClassAsync(string joinCode, int studentId)
        {
            var targetClass = _db.Classes.FirstOrDefault(c => c.JoinCode == joinCode);
            if (targetClass == null)
                return (false, "Mã lớp không tồn tại.");

            bool alreadyJoined = _db.ClassStudents.Any(cs =>
                cs.ClassId == targetClass.Id && cs.StudentId == studentId);

            if (alreadyJoined)
                return (false, "Bạn đã tham gia lớp này rồi.");

            _db.ClassStudents.Add(new ClassStudent
            {
                ClassId = targetClass.Id,
                StudentId = studentId,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public Task<List<ClassAssignment>?> GetAssignmentsForStudentAsync(int classId, int studentId)
        {
            bool isInClass = _db.ClassStudents.Any(cs =>
                cs.ClassId == classId && cs.StudentId == studentId);

            if (!isInClass) return Task.FromResult<List<ClassAssignment>?>(null);

            return Task.FromResult<List<ClassAssignment>?>(
                _db.ClassAssignments
                   .Include(a => a.Exam)
                   .Where(a => a.ClassId == classId)
                   .OrderByDescending(a => a.AssignedAt)
                   .ToList());
        }

        // =====================================================
        // PRIVATE HELPER
        // =====================================================

        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

using ATOZA.Application.DTOs.Class;
using ATOZA.Domain.Entities;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IClassService
    {
        // Teacher
        Task<List<Class>> GetClassesByTeacherAsync(int teacherId);
        Task<Class> CreateClassAsync(CreateClassDto dto, int teacherId);
        Task<Class?> GetClassDetailAsync(int classId, int teacherId);
        Task<AssignExamResultDto> AssignExamAsync(AssignExamDto dto, int teacherId);
        Task<List<ClassAssignment>> GetClassAssignmentsAsync(int classId, int teacherId);
        Task<byte[]?> ExportStudentsCsvAsync(int classId, int teacherId);

        // Student
        Task<List<Class>> GetClassesByStudentAsync(int studentId);
        Task<(bool Success, string? Error)> JoinClassAsync(string joinCode, int studentId);
        Task<List<ClassAssignment>?> GetAssignmentsForStudentAsync(int classId, int studentId);
    }
}

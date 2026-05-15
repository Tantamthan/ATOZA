using ATOZA.Application.DTOs.Exam;
using ATOZA.Domain.Entities;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IExamService
    {
        Task<int> CreateExamAsync(CreateExamDto dto, int creatorId);
        Task<Exam?> GetExamWithQuestionsAsync(int examId);
        Task<StudentExamAccessResultDto> GetExamForStudentAsync(int examId, int studentId);
        Task<bool> HasSubmittedAsync(int examId, int studentId);
        Task<List<Exam>> GetAllExamsAsync();
        Task<List<Exam>> GetExamsByCreatorAsync(int creatorId);
        Task<List<Exam>> GetAssignableExamsForTeacherAsync(int teacherId);
        Task<bool> SetExamVisibilityAsync(int examId, int teacherId, bool isPublic);
        Task<PracticeAnswerResultDto> CheckPracticeAnswerAsync(CheckPracticeAnswerDto dto, int studentId);
        Task<Exam?> GetExamForEditAsync(int examId, int teacherId);
        Task<UpdateExamResultDto> UpdateExamAsync(UpdateExamDto dto, int teacherId);
        Task<byte[]?> ExportExamToWordAsync(int examId, int teacherId);
    }
}

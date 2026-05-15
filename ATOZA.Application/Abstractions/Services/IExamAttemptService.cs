using ATOZA.Application.DTOs.Exam;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IExamAttemptService
    {
        Task<ExamAttemptResultDto> StartAttemptAsync(int examId, int studentId);
        Task<ExamAttemptResultDto> GetActiveAttemptAsync(int examId, int studentId);
    }
}

using ATOZA.Application.DTOs.Exam;
using ATOZA.Domain.Entities;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IExamService
    {
        Task<int> CreateExamAsync(CreateExamDto dto, int creatorId);
        Task<Exam?> GetExamWithQuestionsAsync(int examId);
        Task<bool> HasSubmittedAsync(int examId, int studentId);
        Task<List<Exam>> GetAllExamsAsync();
        Task<List<Exam>> GetExamsByCreatorAsync(int creatorId);
    }
}

using ATOZA.Application.DTOs.Submission;
using ATOZA.Domain.Entities;

namespace ATOZA.Application.Abstractions.Services
{
    public interface ISubmissionService
    {
        Task<SubmitResultDto> SubmitExamAsync(SubmitExamDto dto, int studentId);
        Task<List<Submission>> GetStudentSubmissionsAsync(int studentId);
        Task<Submission?> GetSubmissionDetailAsync(int examId, int studentId);
        Task<List<StudentReportDto>> GetSubmissionReportAsync(int classId, int examId);
    }
}

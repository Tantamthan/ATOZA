using ATOZA.Application.DTOs;
using ATOZA.Domain.Entities;
using ATOZA.Domain.Enums;

namespace ATOZA.Application.Abstractions.Services
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<UserListDto>> GetAllUsersAsync(UserRole? roleFilter = null);
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);
        Task<bool> SetTeacherApprovalStatusAsync(int userId, ApprovalStatus approvalStatus);
        Task<List<Exam>> GetAllExamsWithCreatorAsync();
        Task<bool> DeleteExamAsync(int examId);
        Task<bool> SetExamPublicStatusAsync(int examId, bool isPublic);
        Task<List<ClassOverviewDto>> GetAllClassesOverviewAsync();
    }
}

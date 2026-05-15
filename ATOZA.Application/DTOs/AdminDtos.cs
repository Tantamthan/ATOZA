namespace ATOZA.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalTeachers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalExams { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubmissions { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
    }

    public class UserListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ClassOverviewDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int AssignmentCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

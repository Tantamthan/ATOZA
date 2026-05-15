using ATOZA.Domain.Common;
using ATOZA.Domain.Enums;

namespace ATOZA.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public bool IsActive { get; set; } = true;
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved;

        // Navigation properties
        public ICollection<Class> Classes { get; set; } = new List<Class>();
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    }
}

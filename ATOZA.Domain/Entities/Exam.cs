using ATOZA.Domain.Common;
using ATOZA.Domain.Enums;

namespace ATOZA.Domain.Entities
{
    public class Exam : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CreatorId { get; set; }
        public int DurationMinutes { get; set; }
        public ExamMode ExamMode { get; set; } = ExamMode.Assessment;
        public bool IsPublic { get; set; }
        public int? ParentExamId { get; set; }
        public int VersionNumber { get; set; } = 1;
        public bool IsArchived { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Navigation properties
        public Exam? ParentExam { get; set; }
        public ICollection<Exam> Versions { get; set; } = new List<Exam>();
        public User Creator { get; set; } = null!;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
        public ICollection<ClassAssignment> ClassAssignments { get; set; } = new List<ClassAssignment>();
    }
}

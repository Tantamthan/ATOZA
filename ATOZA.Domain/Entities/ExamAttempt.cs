using ATOZA.Domain.Common;
using ATOZA.Domain.Enums;

namespace ATOZA.Domain.Entities
{
    public class ExamAttempt : BaseEntity
    {
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

        public Exam Exam { get; set; } = null!;
        public User Student { get; set; } = null!;
    }
}

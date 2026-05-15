using ATOZA.Domain.Common;

namespace ATOZA.Domain.Entities
{
    /// <summary>Bài nộp của học sinh cho một đề thi</summary>
    public class Submission : BaseEntity
    {
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public double Score { get; set; }
        public DateTime SubmitTime { get; set; } = DateTime.UtcNow;

        // Navigation
        public Exam Exam { get; set; } = null!;
        public User Student { get; set; } = null!;
        public ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();
    }
}

namespace ATOZA.Domain.Entities
{
    /// <summary>Chi tiết từng câu trả lời trong bài nộp</summary>
    public class SubmissionDetail
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public int QuestionId { get; set; }
        public string? Answer { get; set; }   // "A", "B", "C", "D"
        public bool IsCorrect { get; set; }

        // Navigation
        public Submission Submission { get; set; } = null!;
        public Question Question { get; set; } = null!;
    }
}

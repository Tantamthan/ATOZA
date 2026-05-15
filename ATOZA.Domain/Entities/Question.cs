using ATOZA.Domain.Common;

namespace ATOZA.Domain.Entities
{
    public class Question : BaseEntity
    {
        public int ExamId { get; set; }
        public int OrderNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty; // "A", "B", "C", "D"

        // Navigation
        public Exam Exam { get; set; } = null!;
        public ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();
    }
}

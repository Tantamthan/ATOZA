namespace ATOZA.Application.DTOs.Submission
{
    public class SubmitExamDto
    {
        public int ExamId { get; set; }
        public List<StudentAnswerDto> Answers { get; set; } = new();
    }

    public class StudentAnswerDto
    {
        public int QuestionId { get; set; }
        public string? SelectedOption { get; set; }
    }

    public class SubmitResultDto
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class StudentReportDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsSubmitted { get; set; }
        public double? Score { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}

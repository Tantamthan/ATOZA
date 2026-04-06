namespace ATOZA.Application.DTOs.Exam
{
    public class CreateExamDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamMode { get; set; } = "Assessment";
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class QuestionDto
    {
        public int OrderNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}

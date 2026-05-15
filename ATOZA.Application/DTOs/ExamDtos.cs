namespace ATOZA.Application.DTOs.Exam
{
    public class CreateExamDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamMode { get; set; } = "Assessment";
        public bool IsPublic { get; set; }
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

    public class UpdateExamDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamMode { get; set; } = "Assessment";
        public bool IsPublic { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class StudentExamAccessResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ATOZA.Domain.Entities.Exam? Exam { get; set; }
    }

    public class CheckPracticeAnswerDto
    {
        public int ExamId { get; set; }
        public int QuestionId { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
    }

    public class PracticeAnswerResultDto
    {
        public bool Success { get; set; }
        public bool IsCorrect { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

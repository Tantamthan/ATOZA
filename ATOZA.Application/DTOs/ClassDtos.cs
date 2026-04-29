using System.ComponentModel.DataAnnotations;

namespace ATOZA.Application.DTOs.Class
{
    public class CreateClassDto
    {
        [Required] public string ClassName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class AssignExamDto
    {
        public int ClassId { get; set; }
        public int ExamId { get; set; }
        public DateTime AvailableFrom { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class AssignExamResultDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}

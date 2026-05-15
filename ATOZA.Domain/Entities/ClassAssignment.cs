using ATOZA.Domain.Common;

namespace ATOZA.Domain.Entities
{
    /// <summary>Giáo viên giao đề thi cho lớp học</summary>
    public class ClassAssignment : BaseEntity
    {
        public int ClassId { get; set; }
        public int ExamId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime AvailableFrom { get; set; }
        public DateTime DueDate { get; set; }

        // Navigation
        public Class Class { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
    }
}

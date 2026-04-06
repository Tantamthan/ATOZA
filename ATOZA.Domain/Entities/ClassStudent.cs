namespace ATOZA.Domain.Entities
{
    /// <summary>Học sinh tham gia lớp học</summary>
    public class ClassStudent
    {
        public int ClassId { get; set; }
        public int StudentId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Class Class { get; set; } = null!;
        public User Student { get; set; } = null!;
    }
}

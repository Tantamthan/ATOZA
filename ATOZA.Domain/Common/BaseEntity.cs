namespace ATOZA.Domain.Common
{
    /// <summary>
    /// Lớp cơ sở cho tất cả Entity – chứa Id và thời gian tạo
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

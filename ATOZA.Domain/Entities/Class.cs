using ATOZA.Domain.Common;

namespace ATOZA.Domain.Entities
{
    public class Class : BaseEntity
    {
        public string ClassName { get; set; } = string.Empty;
        public int TeacherId { get; set; }
        public string JoinCode { get; set; } = string.Empty;

        // Navigation
        public User Teacher { get; set; } = null!;
        public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public ICollection<ClassAssignment> ClassAssignments { get; set; } = new List<ClassAssignment>();
    }
}

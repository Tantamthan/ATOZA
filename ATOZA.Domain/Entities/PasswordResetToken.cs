using ATOZA.Domain.Common;

namespace ATOZA.Domain.Entities
{
    public class PasswordResetToken : BaseEntity
    {
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? UsedAtUtc { get; set; }

        public User User { get; set; } = null!;
    }
}

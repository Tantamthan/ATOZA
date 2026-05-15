using System.ComponentModel.DataAnnotations;

namespace ATOZA.Application.DTOs
{
    public class LoginDto
    {
        [Required] public string UserName { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterDto : IValidatableObject
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public bool IsExternalLogin { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsExternalLogin)
                yield break;

            if (string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult(
                    "Vui long nhap mat khau.",
                    new[] { nameof(Password) });
                yield break;
            }

            if (Password.Length < 6)
            {
                yield return new ValidationResult(
                    "Mat khau phai co it nhat 6 ky tu.",
                    new[] { nameof(Password) });
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "Mat khau xac nhan khong khop.",
                    new[] { nameof(ConfirmPassword) });
            }
        }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace ATOZA.Application.DTOs
{
    public class LoginDto
    {
        [Required] public string UserName { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto : IValidatableObject
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult(
                    "Vui lòng nhập mật khẩu mới.",
                    new[] { nameof(Password) });
                yield break;
            }

            if (Password.Length < 6)
            {
                yield return new ValidationResult(
                    "Mật khẩu phải có ít nhất 6 ký tự.",
                    new[] { nameof(Password) });
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "Mật khẩu xác nhận không khớp.",
                    new[] { nameof(ConfirmPassword) });
            }
        }
    }

    public class PasswordResetRequestResultDto
    {
        public bool EmailExists { get; set; }
        public string? ResetToken { get; set; }
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
                    "Vui lòng nhập mật khẩu.",
                    new[] { nameof(Password) });
                yield break;
            }

            if (Password.Length < 6)
            {
                yield return new ValidationResult(
                    "Mật khẩu phải có ít nhất 6 ký tự.",
                    new[] { nameof(Password) });
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                yield return new ValidationResult(
                    "Mật khẩu xác nhận không khớp.",
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

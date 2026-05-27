namespace ATOZA.Application.Abstractions.Services
{
    public interface IEmailService
    {
        Task SendRegistrationSuccessAsync(
            string toEmail,
            string fullName,
            string role,
            bool isPendingApproval,
            CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(
            string toEmail,
            string fullName,
            string resetUrl,
            CancellationToken cancellationToken = default);

        Task SendPasswordChangedAsync(
            string toEmail,
            string fullName,
            DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken = default);
    }
}

namespace ATOZA.Application.Abstractions.Services
{
    public interface IEmailSender
    {
        bool IsConfigured { get; }
        Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    }
}

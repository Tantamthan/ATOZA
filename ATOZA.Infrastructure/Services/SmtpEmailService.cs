using System.Net;
using System.Net.Mail;
using System.Text;
using ATOZA.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATOZA.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendRegistrationSuccessAsync(
            string toEmail,
            string fullName,
            string role,
            bool isPendingApproval,
            CancellationToken cancellationToken = default)
        {
            await SendAsync(
                toEmail,
                fullName,
                "Dang ky tai khoan Atoza thanh cong",
                BuildRegistrationMessage(fullName, role, isPendingApproval),
                cancellationToken);
        }

        public async Task SendPasswordResetAsync(
            string toEmail,
            string fullName,
            string resetUrl,
            CancellationToken cancellationToken = default)
        {
            await SendAsync(
                toEmail,
                fullName,
                "Dat lai mat khau Atoza",
                BuildPasswordResetMessage(fullName, resetUrl),
                cancellationToken);
        }

        public async Task SendPasswordChangedAsync(
            string toEmail,
            string fullName,
            DateTimeOffset changedAtUtc,
            CancellationToken cancellationToken = default)
        {
            await SendAsync(
                toEmail,
                fullName,
                "Mat khau tai khoan Atoza vua duoc thay doi",
                BuildPasswordChangedMessage(fullName, changedAtUtc),
                cancellationToken);
        }

        private async Task SendAsync(
            string toEmail,
            string fullName,
            string subject,
            string body,
            CancellationToken cancellationToken)
        {
            var host = _configuration["Email:SmtpHost"];
            var fromEmail = _configuration["Email:FromEmail"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogInformation("Registration email skipped because SMTP is not configured.");
                return;
            }

            try
            {
                var port = int.TryParse(_configuration["Email:SmtpPort"], out var configuredPort)
                    ? configuredPort
                    : 587;
                var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var configuredEnableSsl)
                    || configuredEnableSsl;
                var userName = _configuration["Email:UserName"];
                var password = _configuration["Email:Password"]?.Replace(" ", string.Empty);
                var fromName = _configuration["Email:FromName"] ?? "Atoza";

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName, Encoding.UTF8),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8
                };

                message.To.Add(new MailAddress(toEmail, fullName, Encoding.UTF8));

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                    client.Credentials = new NetworkCredential(userName.Trim(), password);

                cancellationToken.ThrowIfCancellationRequested();
                await client.SendMailAsync(message, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending email '{Subject}' to {Email} was canceled.", subject, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email '{Subject}' to {Email}.", subject, toEmail);
            }
        }

        private static string BuildRegistrationMessage(string fullName, string role, bool isPendingApproval)
        {
            var greetingName = string.IsNullOrWhiteSpace(fullName) ? "ban" : fullName.Trim();
            var statusMessage = isPendingApproval
                ? "Tai khoan giao vien cua ban da duoc tao va dang cho Admin duyet."
                : "Tai khoan cua ban da san sang su dung.";

            return $"""
                   Xin chao {greetingName},

                   Ban da dang ky tai khoan Atoza thanh cong.
                   Vai tro: {role}
                   Trang thai: {statusMessage}

                   Neu ban khong thuc hien dang ky nay, vui long bo qua email nay.

                   Atoza
                   """;
        }

        private static string BuildPasswordChangedMessage(string fullName, DateTimeOffset changedAtUtc)
        {
            var greetingName = string.IsNullOrWhiteSpace(fullName) ? "ban" : fullName.Trim();
            var localTime = changedAtUtc.ToOffset(TimeSpan.FromHours(7));

            return $"""
                   Xin chao {greetingName},

                   Mat khau tai khoan Atoza cua ban vua duoc thay doi luc {localTime:HH:mm dd/MM/yyyy} (gio Viet Nam).

                   Neu khong phai ban thuc hien thay doi nay, vui long lien he ngay voi quan tri vien va dat lai mat khau qua chuc nang Quen mat khau.

                   Atoza
                   """;
        }

        private static string BuildPasswordResetMessage(string fullName, string resetUrl)
        {
            var greetingName = string.IsNullOrWhiteSpace(fullName) ? "ban" : fullName.Trim();

            return $"""
                   Xin chao {greetingName},

                   Ban vua yeu cau dat lai mat khau tai khoan Atoza.
                   Vui long mo lien ket sau de tao mat khau moi:

                   {resetUrl}

                   Lien ket nay se het han sau 30 phut. Neu ban khong yeu cau dat lai mat khau, vui long bo qua email nay.

                   Atoza
                   """;
        }
    }
}

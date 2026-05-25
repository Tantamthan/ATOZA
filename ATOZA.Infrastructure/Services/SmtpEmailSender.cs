using ATOZA.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ATOZA.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(GetSetting("Host")) &&
            !string.IsNullOrWhiteSpace(GetSetting("UserName")) &&
            !string.IsNullOrWhiteSpace(GetSetting("Password"));

        public async Task SendPasswordResetAsync(
            string toEmail,
            string resetLink,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Email SMTP chưa được cấu hình.");

            using var message = new MailMessage
            {
                From = new MailAddress(GetSetting("FromEmail") ?? GetSetting("UserName")!, GetSetting("FromName") ?? "Atoza"),
                Subject = "Đặt lại mật khẩu Atoza",
                Body = BuildPasswordResetBody(resetLink),
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(GetSetting("Host")!, GetIntSetting("Port", 587))
            {
                EnableSsl = GetBoolSetting("EnableSsl", true),
                Credentials = new NetworkCredential(GetSetting("UserName"), GetSetting("Password"))
            };

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
        }

        private string? GetSetting(string key) =>
            _configuration[$"Smtp:{key}"] ?? _configuration[$"Email:Smtp:{key}"];

        private int GetIntSetting(string key, int defaultValue) =>
            int.TryParse(GetSetting(key), out var value) ? value : defaultValue;

        private bool GetBoolSetting(string key, bool defaultValue) =>
            bool.TryParse(GetSetting(key), out var value) ? value : defaultValue;

        private static string BuildPasswordResetBody(string resetLink) =>
            $"""
            <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản Atoza.</p>
            <p>Vui lòng bấm vào liên kết bên dưới để tạo mật khẩu mới. Liên kết có hiệu lực trong 30 phút.</p>
            <p><a href="{WebUtility.HtmlEncode(resetLink)}">Đặt lại mật khẩu</a></p>
            <p>Nếu bạn không yêu cầu thao tác này, vui lòng bỏ qua email này.</p>
            """;
    }
}

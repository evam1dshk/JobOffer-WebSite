using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;
using System.Net;

namespace JobListingSite.Web.Models.Mail
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtp = _config.GetSection("SmtpSettings");

            string fromEmail = smtp["FromEmail"];
            string password = smtp["Password"];
            string host = smtp["Host"];
            string portStr = smtp["Port"];
            string enableSslStr = smtp["EnableSsl"];

            // ✅ Validate all settings
            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr))
            {
                throw new InvalidOperationException("SMTP settings are missing in appsettings.json");
            }

            int port = int.Parse(portStr);
            bool enableSsl = bool.Parse(enableSslStr ?? "true");

            using var client = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = enableSsl
            };

            var mail = new MailMessage(fromEmail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }
    }
}

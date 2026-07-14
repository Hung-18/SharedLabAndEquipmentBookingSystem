using Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["EmailSettings:FromEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            // Validate and read email configuration with safe fallbacks
            var host = _config["EmailSettings:Host"];
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("Missing configuration: EmailSettings:Host");

            int port;
            var portConfig = _config.GetValue<int?>("EmailSettings:Port");
            if (portConfig.HasValue)
            {
                port = portConfig.Value;
            }
            else if (int.TryParse(_config["EmailSettings:Post"], out var parsedPost))
            {
                // tolerate common typo 'Post' -> 'Port'
                port = parsedPost;
            }
            else
            {
                // default to 587 if not configured
                port = 587;
            }

            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Missing email credentials in configuration (EmailSettings:Username / EmailSettings:Password).");

            await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}

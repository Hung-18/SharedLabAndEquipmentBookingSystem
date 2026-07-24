using Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Infrastructure.Repository.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration) => _configuration = configuration;

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            Console.WriteLine(">>> Đã vào hàm SendEmailAsync!");
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Email người nhận không được để trống.");

            string fromEmail = Required("EmailSettings:FromEmail");

            Console.WriteLine($">>> Email gửi từ: {fromEmail}");

            string host = Required("EmailSettings:Host");
            string username = Required("EmailSettings:Username");
            string password = Required("EmailSettings:Password");
            int port = _configuration.GetValue<int?>("EmailSettings:Port")
                ?? throw new InvalidOperationException("Thiếu cấu hình EmailSettings:Port.");

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            try
            {
                await smtp.AuthenticateAsync(username, password);
            }
            catch(AuthenticationException ex)
            {
                Console.WriteLine("Auth failed: " + ex.Message);
                Console.WriteLine(ex.ToString()); // includes inner exception / server response
                throw;
            }
            await smtp.SendAsync(email);
            Console.WriteLine(">>> Gửi mail thành công!");
            await smtp.DisconnectAsync(true);
        }

        private string Required(string key) =>
            _configuration[key]
            ?? throw new InvalidOperationException($"Thiếu cấu hình {key}.");
    }
}

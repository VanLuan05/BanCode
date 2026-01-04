using System.Net;
using System.Net.Mail;

namespace BanCode.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            var mailSection = _configuration.GetSection("EmailSettings");

            var client = new SmtpClient(mailSection["MailServer"], int.Parse(mailSection["MailPort"]))
            {
                Credentials = new NetworkCredential(mailSection["SenderEmail"], mailSection["Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(mailSection["SenderEmail"], mailSection["SenderName"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true // Gửi nội dung dạng HTML đẹp
            };

            mailMessage.To.Add(email);

            return client.SendMailAsync(mailMessage);
        }
    }
}
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TrendyKart.Models;
namespace TrendyKart.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var mail = new MailMessage();
            mail.From = new MailAddress(_smtpSettings.Email);
            mail.To.Add(toEmail);
            mail.Subject = subject;
            mail.Body = message;
            mail.IsBodyHtml = true;
            using (var smtp = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
            {
                smtp.Credentials = new NetworkCredential(_smtpSettings.Email, _smtpSettings.Password);
                smtp.EnableSsl = _smtpSettings.EnableSsl;
                await smtp.SendMailAsync(mail);
            }
        }
    }
}

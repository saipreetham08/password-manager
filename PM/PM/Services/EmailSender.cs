using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;

namespace PM.Services
{
    public class SMTPConfiguration
    {
        public string From { get; set; }
        public string SMTPServer { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }
    public class EmailSender : IEmailSender
    {
        private readonly SMTPConfiguration _smtpConfiguration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(SMTPConfiguration sMTPConfiguration, ILogger<EmailSender> logger)
        {
            _smtpConfiguration = sMTPConfiguration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_smtpConfiguration.From);
            mailMessage.To.Add(new MailAddress(toEmail));
            mailMessage.Subject = subject;
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = message;

            SmtpClient client = new SmtpClient(_smtpConfiguration.SMTPServer, _smtpConfiguration.Port);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(_smtpConfiguration.Username, _smtpConfiguration.Password);

            try
            {
                client.Send(mailMessage);
            }
            catch(Exception ex) 
            {
                _logger.LogError("Email sending failed");
                System.Console.WriteLine(ex.Message);
            }
        }
    }
}

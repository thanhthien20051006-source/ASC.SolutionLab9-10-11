using ASC.Solution.Services;
using ASC.Web.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ASC.Web.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly IOptions<ApplicationSettings> _settings;

        public AuthMessageSender(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("admin", _settings.Value.SMTPAccount));
            emailMessage.To.Add(new MailboxAddress("user", email));
            emailMessage.Subject = subject;

            // Nếu message có HTML link reset password thì dùng html
            emailMessage.Body = new TextPart("html")
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(
                    _settings.Value.SMTPServer,
                    _settings.Value.SMTPPort,
                    false);

                await client.AuthenticateAsync(
                    _settings.Value.SMTPAccount,
                    _settings.Value.SMTPPassword);

                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Chưa dùng SMS trong lab nên để trống
            return Task.CompletedTask;
        }
    }
}
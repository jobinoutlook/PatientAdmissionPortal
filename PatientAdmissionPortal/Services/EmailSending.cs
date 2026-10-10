    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Options;
    using MimeKit;

    namespace PatientAdmissionPortal.Services;

    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "Patient Admission Portal";
    }

    public interface IAppEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }

    // Real sender, used when Email:Host is configured.
    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly EmailSettings _s;
        public SmtpEmailSender(IOptions<EmailSettings> options) => _s = options.Value;

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_s.FromName, _s.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_s.Host, _s.Port,
                _s.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(_s.Username))
                await client.AuthenticateAsync(_s.Username, _s.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }

    // Development fallback: no SMTP configured, so the email is printed to the console instead.
    public class ConsoleEmailSender : IAppEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _log;
        public ConsoleEmailSender(ILogger<ConsoleEmailSender> log) => _log = log;

        public Task SendAsync(string to, string subject, string htmlBody)
        {
            _log.LogWarning("DEV EMAIL (SMTP not configured)\nTo: {To}\nSubject: {Subject}\n{Body}", to, subject, htmlBody);
            return Task.CompletedTask;
        }
    }


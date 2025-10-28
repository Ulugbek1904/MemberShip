using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Brokers.Email;

public class MailBroker : IMailBroker
{
    private readonly MailConfig _mailConfig;

    public MailBroker(IOptions<MailConfig> mailConfig)
    {
        _mailConfig = mailConfig.Value;
    }

    public async Task SendEmailAsync(string emails, string messageTitle, string messageBody)
    {
        MimeMessage email = new();
        email.Sender = MailboxAddress.Parse(_mailConfig.Sender);
        email.To.Add(MailboxAddress.Parse(emails));
        email.Subject = messageTitle;

        BodyBuilder bodyBuilder = new()
        {
            HtmlBody = messageBody
        };
        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(_mailConfig.Host, _mailConfig.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailConfig.Login, _mailConfig.Password);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}

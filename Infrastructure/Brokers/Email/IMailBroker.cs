namespace Infrastructure.Brokers.Email;

public interface IMailBroker
{
    Task SendEmailAsync(string email, string messageTitle, string messageBody);
}

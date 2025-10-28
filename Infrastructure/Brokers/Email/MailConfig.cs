namespace Infrastructure.Brokers.Email;

public class MailConfig
{
    public string Login { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Sender { get; set; } = default!;
    public string Host { get; set; } = default!;
    public int Port { get; set; }
}

namespace AuthService.Contracts.Auth;

public class TokenResult
{
    public string AccessToken { get; set; } = default!;
    public DateTime ExpireDate { get; set; }
}

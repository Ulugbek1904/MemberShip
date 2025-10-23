using AuthService.Contracts.Auth;
using AuthService.Interfaces;
using Domain.Models.Auth;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;

namespace AuthService;

public class AuthService : IAuthService
{
    private readonly IConfiguration configuratoin;

    public AuthService(IConfiguration _configuratoin)
    {
        configuratoin = _configuratoin;
    }




    #region ACCESS TOKEN 
    private Task<TokenResult> GenerateAccessToken(long userid)
    {
        var user = GetUser(userid);





        var authSection = configuratoin.GetRequiredSection("Auth");
        var issuer = authSection.GetValueOrThrowsNotFound<string>("Issuer");
        var audience = authSection.GetValueOrThrowsNotFound<string>("Audience");
        var secretKey = authSection.GetValueOrThrowsNotFound<string>("SecretKey");
        var expireTimeInMinutes = authSection.GetValueOrThrowsNotFound<int>("ExpireInMinutes");
        var expireDate = DateTime.Now.AddMinutes(expireTimeInMinutes);


    }
    #endregion
    #region REFRESH TOKEN
    #endregion
    #region HELPER METHODS
    private async Task<User> GetUser(long userId)
    {
        throw new NotImplementedException();
    }
    #endregion
}

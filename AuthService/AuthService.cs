using AuthService.Contracts.Auth;
using AuthService.Extensions;
using AuthService.Interfaces;
using Common.Common.Helpers;
using Common.EF.Attributes;
using Domain.Constants;
using Domain.Extensions;
using Domain.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService;

[Injectable]
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _contextAccessor;

    public AuthService(
        IConfiguration configuration,
        IHttpContextAccessor contextAccessor)
    {
        _configuration = configuration;
        _contextAccessor = contextAccessor;
    }




    #region ACCESS TOKEN 
    private async Task<TokenResult> GenerateAccessToken(long userid)
    {
        var user = await GetUser(userid);

        var authSection = configuratoin.GetRequiredSection("Auth");
        var issuer = authSection.GetValueOrThrowsNotFound<string>("Issuer");
        var audience = authSection.GetValueOrThrowsNotFound<string>("Audience");
        var secretKey = authSection.GetValueOrThrowsNotFound<string>("SecretKey");
        var expireTimeInMinutes = authSection.GetValueOrThrowsNotFound<int>("ExpireInMinutes");
        var expireDate = DateTime.Now.AddMinutes(expireTimeInMinutes);

        List<Claim> claims = new()
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Authentication, "JWT Bearer"),
            new(ClaimTypes.Expiration, expireDate.ToString("yyyy.mm.dd HH:mm:ss")),
            new(CustomClaimNames.Permissions, string.Join(", ", result.Permissions)),
            new(CustomClaimNames.UserId, user.Id.ToString()),
            new(CustomClaimNames.UserRoleId, user.UserRoleId?.ToString() ?? ""),
            new(CustomClaimNames.StructureCode, result.StructureId),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expireDate,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new()
        {
            AccessToken = jwt,
            ExpireDate = expireDate,
        };
    }
    #endregion
    #region REFRESH TOKEN

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToBase64String(randomBytes);
    }
    private async Task SaveRefreshTokenAsync(long userId, bool isMobile = false)
    {
        await _context.RefreshTokens
            .Where(x => !x.IsRevoked && x.UserId == userId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(v => v.IsRevoked, true));

        var refreshToken = GenerateRefreshToken();
        var tokenHash = PasswordHelper.Encrypt(refreshToken);

        var authSection = _configuration.GetRequiredSection("Auth");
        var expirationInMinutes = authSection.GetValueOrThrowsNotFound<int>("RefreshExpireInMinutes");
        var mobileExpirationInMinutes = authSection.GetValueOrThrowsNotFound<int>("MobileRefreshExpireInMinutes");

        RefreshToken newToken = new()
        {
            TokenHash = tokenHash,
            UserId = userId,
            ExpiryDate = DateTime.Now.AddMinutes(isMobile ? mobileExpirationInMinutes : expirationInMinutes),
            IsRevoked = false,
        };

        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync();

        _contextAccessor.HttpContext!.SetCookie(
            "refresh-token",
            refreshToken,
            newToken.ExpiryDate);
    }
    private async Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = PasswordHelper.Encrypt(refreshToken);

        return await _context.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash && !rt.IsRevoked);
    }
    #endregion
    #region HELPER METHODS
    private async Task<User> GetUser(long userId)
    {
        throw new NotImplementedException();
    }
    #endregion
}

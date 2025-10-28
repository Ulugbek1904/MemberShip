using AuthService.Contracts.Auth;
using AuthService.Extensions;
using AuthService.Interfaces;
using Common.Common.Helpers;
using Common.EF.Attributes;
using Common.Exceptions.Common;
using Domain.Constants;
using Domain.Extensions;
using Domain.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.SecurityTokenService;
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
    public async Task<TokenResult> RegisterAsync(MobileRegisterDto dto)
    {
        dto.PhoneNumber = dto.PhoneNumber.GetCorrectPhoneNumber();

        if (await _context.Users.AnyAsync(x => x.PhoneNumber == dto.PhoneNumber && !x.IsDeleted))
            throw new AlreadyExistsException("Phone_number_already_exists");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await this.VerifyOtpCodeAsync(new()
            {
                Otp = dto.Otp,
                PhoneNumber = dto.PhoneNumber,
                Trusted = false
            }, true);

            User user = new()
            {
                Name = OtherConstants.MOBILE_GUEST,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = EnvironmentHelper.IsProduction
                    ? PasswordHelper.Encrypt(dto.Password)
                    : dto.Password,
                StatusId = (long)EnumStatusUser.ConfirmedWithOtp,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            await this.CreateUserDevice(user.Id, null, dto.PhoneBrand);
            await this.SaveRefreshTokenAsync(user.Id, null, true);

            await transaction.CommitAsync();

            return await this.GenerateTokenAsync(user.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<bool> CheckPhoneRegisteredAsync(string phoneNumber)
    {
        phoneNumber = phoneNumber.GetCorrectPhoneNumber();

        return await _context.Users
            .AnyAsync(x => x.PhoneNumber == phoneNumber && !x.IsDeleted);
    }

    #region WEB
    public async Task RegisterAsync(RegisterDto dto)
    {
        var isContactVerified = _contextAccessor.HttpContext!
            .GetOrThrowExceptionCookie(_contactVerifiedCookieKey) == "true";

        if (!isContactVerified)
            throw new UnauthorizedException("Please_verify_your_contact_before_registering");

        var normalized = dto.PhoneNumber.GetCorrectPhoneNumber();

        if (await CheckPhoneRegisteredAsync(normalized))
            throw new BadRequestException("This_phone_number_already_exists");

        var verifyContent = await _styxClientService.VerifyAndGetCmsContentAsync(dto.Cms);

        if (!verifyContent.IsVerified)
            throw new BadRequestException("Provided_does_not_match_the_verified_data.");

        string innPinfl = verifyContent.Inn;

        var hasDirectorConflict = await _context.UserToCompanies
            .AnyAsync(x => x.Company.InnPinfl == innPinfl && x.UserType == EnumUserType.Director);

        if (hasDirectorConflict)
            throw new BadRequestException($"Director_already_exist_that_{innPinfl}");

        var client = await GetBankClientAsync(innPinfl, false);
        var extra = await GetExtraAsync(new()
        {
            ["Filial"] = client!.BranchId,
            ["ClientType"] = client.Typeof,
            ["District"] = client.DistrictCode,
            ["Region"] = client.RegionCode
        });
        var cmsUrl = await SaveCmsAsync(dto, innPinfl);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(x => x.InnPinfl == innPinfl && !x.IsDeleted);

            if (company is null)
            {
                company = CreateCompany(dto, client, cmsUrl, isMobile: false, isWeb: true);
                SetExtraIDs(company, extra);
                _context.Companies.Add(company);
            }
            else
            {
                var to = await _context.UserToCompanies
                    .Where(x => x.CompanyId == company.Id && x.UserType == EnumUserType.EImzo)
                    .AsTracking()
                    .SingleOrDefaultAsync();

                if (to is not null)
                {
                    await _context.RefreshTokens
                        .Where(t => t.UserId == to.UserId &&
                                    t.User.StatusId == (long)EnumStatusUser.SignedByEImzo)
                        .ExecuteDeleteAsync();

                    await _context.Users
                        .Where(u => u.Id == to.UserId && u.StatusId == (long)EnumStatusUser.SignedByEImzo)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(u => u.IsDeleted, true)
                            .SetProperty(u => u.UpdatedAt, DateTime.Now)
                        );
                }

                company = UpdateCompany(company, dto, client, cmsUrl, isMobile: false, isWeb: true);
                SetExtraIDs(company, extra);
                _context.Companies.Update(company);
            }

            await _context.SaveChangesAsync();

            User director = new()
            {
                Name = client.Director ?? client.Name,
                PhoneNumber = normalized,
                PasswordHash = EnvironmentHelper.IsProduction
                    ? PasswordHelper.Encrypt(dto.Password)
                    : dto.Password,
                UserRoleId = null,
                StatusId = (long)EnumStatusUser.PreRegistration,
                CompanyId = company.Id,
                IsDirector = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Users.Add(director);
            await _context.SaveChangesAsync();

            var utc = CreateUserToCompany(director.Id, company.Id, EnumUserType.Director);
            _context.UserToCompanies.Add(utc);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<object> VerifyOtpAfterLoginAsync(VerifyOtpDto dto)
    {
        var verificationKey = _contextAccessor.HttpContext!
            .GetOrThrowExceptionCookie(_otpCookieKey);

        var verificationCode = await _context.VerificationCodes
            .FirstOrDefaultAsync(x => x.Key == verificationKey && x.Otp == dto.Otp.Hash());

        if (verificationCode is null)
            throw new UnauthorizedException();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            verificationCode.HasUsed = true;
            await _context.SaveChangesAsync();

            dto.PhoneNumber = dto.PhoneNumber.GetCorrectPhoneNumber();

            var selecter = await _context.Users
                .Where(u => u.PhoneNumber == dto.PhoneNumber && !u.IsDeleted)
                .Select(u => new
                {
                    User = u,
                    CompanyIds = u.UserToCompanies.Select(c => c.CompanyId).ToList()
                })
                .SingleOrDefaultAsync();

            var user = selecter?.User;
            if (user is null)
                throw new UnauthorizedException();

            var companyIds = selecter!.CompanyIds;

            switch (companyIds.Count)
            {
                case 0:
                    throw new UnauthorizedException();
                case > 1:
                    return await GetWantChooseCompaniesAsync(user.Id);
            }

            var companyId = companyIds[0];

            if (dto.Trusted)
                await this.CreateUserDevice(user.Id, companyId, "");

            await this.MarkClientPlatformAsync(companyId, false);

            await this.SaveRefreshTokenAsync(user.Id, companyId);
            await transaction.CommitAsync();

            return await this.GenerateTokenAsync(user.Id, companyId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    #endregion

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

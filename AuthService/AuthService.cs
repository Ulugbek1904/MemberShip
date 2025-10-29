using AuthService.Contracts.Auth;
using AuthService.Contracts.Auth.Both;
using AuthService.Extensions;
using AuthService.Interfaces;
using Common.Common.Helpers;
using Common.EF.Attributes;
using Common.Exceptions;
using Common.Exceptions.Common;
using Domain.Constants;
using Domain.Enum.Auth;
using Domain.Extensions;
using Domain.Models.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService;

[Injectable]
public partial class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly AppDbContext _context;
    private readonly FileConfig fileConfig;

    public AuthService(
        IConfiguration configuration,
        IHttpContextAccessor contextAccessor,
        AppDbContext context,
        IOptions<FileConfig> fileConfig)
    {
        _configuration = configuration;
        _contextAccessor = contextAccessor;
        this._context = context;
        this.fileConfig = fileConfig;
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

    #region WEB and MOBILE
    public async Task<object> SignAsync(SignDto dto)
    {
        var selecter = await _context.Users
            .Where(x => x.Email == dto.Email && !x.IsDeleted)
            .Select(u => new
            {
                User = u,
                CompanyIds = u.UserToCompanies.Select(c => c.CompanyId).ToList()
            })
            .SingleOrDefaultAsync();

        var user = selecter?.User;
        if (user is null)
            throw new NotFoundException("User_not_found_or_deleted");

        if (!user.IsValidPassword(dto.Password))
            throw new BadRequestException("Password_is_incorrect!");

        if (dto.IsMobile)
        {
            return await SendSmsAsync(new()
            {
                PhoneNumber = dto.PhoneNumber,
                TemplateId = SmsMessages.LOGIN_CODE_TEMPLATE_ID,
                Hash = dto.Hash
            });
        }

        var companyIds = selecter!.CompanyIds;
        if (companyIds.Count == 0)
            throw new UnauthorizedException();

        var userId = user.Id;

        var deviceId = GetOrCreateDeviceToken();
        var isDeviceTrusted = await IsDeviceTrustedAsync(user.Id, deviceId);

        if (isDeviceTrusted)
        {
            if (companyIds.Count > 1)
                return await GetWantChooseCompaniesAsync(userId);

            await SaveRefreshTokenAsync(userId, companyIds[0]);
            return await GenerateTokenAsync(userId, companyIds[0]);
        }

        var directorPhoneNumber = await (
                                      from utc in _context.UserToCompanies
                                      join director in _context.UserToCompanies
                                          on utc.CompanyId equals director.CompanyId
                                      where utc.UserId == userId && director.UserType == EnumUserType.Director
                                      select director.User.PhoneNumber
                                  ).FirstOrDefaultAsync()
                                  ?? throw new UnauthorizedException();

        return await SendSmsAsync(new()
        {
            PhoneNumber = directorPhoneNumber,
            TemplateId = SmsMessages.LOGIN_CODE_TEMPLATE_ID
        });
    }
    public async Task<object> GetMeAsync(long userId, string? companyId)
    {
        var user = await _context.Users
                       .Where(x => x.Id == userId && !x.IsDeleted)
                       .Include(x => x.UserRole)
                       .Include(x => x.Person)
                       .Include(x => x.Status)
                       .AsNoTracking()
                       .FirstOrDefaultAsync()
                   ?? throw new NotFoundException("User not found");

        object? companyDto = null;
        bool isDirector = false;

        UserToCompany utc;
        if (string.IsNullOrWhiteSpace(companyId))
        {
            utc = await _context.UserToCompanies
                .Where(x => x.UserId == userId && x.UserType == EnumUserType.Director)
                .AsTracking()
                .FirstOrDefaultAsync();
        }
        else
        {
            utc = await _context.UserToCompanies
                .Where(x => x.UserId == userId && x.CompanyId == long.Parse(companyId))
                .AsTracking()
                .SingleOrDefaultAsync();
        }

        if (utc is not null)
        {
            var company = await _context.Companies
                .Where(x => x.Id == utc.CompanyId)
                .Include(x => x.TypeClient)
                .Include(x => x.Structure)
                .Include(x => x.BankFilial)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (company != null)
            {
                companyDto = new
                {
                    company.Id,
                    company.Address,
                    company.Name,
                    company.BankClientId,
                    Inn = company.InnPinfl,
                    company.ClientCode,
                    company.CodeFilial,
                    company.DateOpen,
                    company.ClientsId,
                    company.BranchId,
                    company.LocalCode,
                    company.Subject,
                    company.RegistrationNumber,
                    company.Director,
                    company.DirectorPassport,
                    company.MainAccount,
                    company.MobilePhone,
                    company.DistrictCode,
                    company.Oked,
                    DisposableToken = CreateDisposableToken(company.Id),
                    BankFilial = company.BankFilialId != null
                        ? new
                        {
                            Id = company.BankFilialId,
                            company.BankFilial?.NameBXOBXKM,
                            company.BankFilial?.BankName,
                            company.BankFilial?.CbCode
                        }
                        : null,
                    TypeClient = company.TypeClientId.HasValue
                        ? new
                        {
                            company.TypeClient?.Id,
                            company.TypeClient?.Code,
                            company.TypeClient?.Name,
                        }
                        : null,
                    Structure = company.StructureId != null
                        ? new
                        {
                            company.StructureId,
                            company.Structure!.Name
                        }
                        : null
                };

                isDirector = utc is { UserType: EnumUserType.Director };
            }
        }

        return new
        {
            Company = companyDto,
            User = new
            {
                user.Id,
                user.Name,
                user.StatusId,
                Status = user.Status?.Name,
                user.UserRoleId,
                RoleName = user.UserRole?.Name,
                user.PhoneNumber,
                IsDirector = isDirector,
                Person = user.PersonId == null
                    ? null
                    : new
                    {
                        Id = user.PersonId,
                        user.Person?.FullName,
                        user.Person?.Pinfl
                    }
            }
        };
    }
    public async Task<TokenResult> GenerateTokenChoosenCompanyAsync(ChoosenCompanyDto dto)
    {
        var userIdCookie = _contextAccessor.HttpContext!.Request.Cookies["choosen-user-id"];

        if (string.IsNullOrWhiteSpace(userIdCookie))
            throw new BadRequestException("invalid_or_expired_user_id");

        var userId = long.Parse(userIdCookie);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var ent = await _context.UserToCompanies
                      .Where(x => x.UserId == userId && x.CompanyId == dto.CompanyId)
                      .AsNoTracking()
                      .SingleOrDefaultAsync()
                  ?? throw new NotFoundException("User-to-Company_not_found");

        await SaveRefreshTokenAsync(ent.UserId, dto.CompanyId, true);

        if (dto.Trusted)
            await this.CreateUserDevice(userId, dto.CompanyId, "");

        await transaction.CommitAsync();

        return await GenerateTokenAsync(ent.UserId, ent.CompanyId);
    }
    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        string phone = dto.PhoneNumber.GetCorrectPhoneNumber();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var verificationKey = _contextAccessor.HttpContext!
                .GetOrThrowExceptionCookie(_otpCookieKey);

            var otp = EnvironmentHelper.IsProduction ? dto.Otp.Hash() : dto.Otp;

            var verificationCode = await _context.VerificationCodes
                .FirstOrDefaultAsync(x => x.Key == verificationKey && x.Otp == otp);

            if (verificationCode is null)
                throw new BadRequestException("Verification_code_is_incorrect");

            verificationCode.HasUsed = true;
            await _context.SaveChangesAsync();

            var user = await _context.Users
                .Where(x => x.PhoneNumber == phone && !x.IsDeleted)
                .SingleOrDefaultAsync();

            if (user is null)
            {
                throw new NotFoundException("User_not_foud");
            }

            user.PasswordHash = EnvironmentHelper.IsProduction
                ? PasswordHelper.Encrypt(dto.Password)
                : dto.Password;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task LogoutAsync(long userId)
    {
        await _context.RefreshTokens
            .Where(x => !x.IsRevoked && x.UserId == userId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(v => v.IsRevoked, true));

        _contextAccessor.HttpContext!.Response.Cookies.Delete("refresh-token");
    }
    public async Task<TokenResult> RefreshTokenAsync()
    {
        var refreshToken = _contextAccessor.HttpContext!
            .GetCookie("refresh-token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new UnauthorizedException();
        }

        var storedToken = await GetRefreshTokenAsync(refreshToken);

        if (storedToken is null || storedToken.ExpiryDate < DateTime.Now)
        {
            throw new UnauthorizedException();
        }

        return await GenerateTokenAsync(storedToken.UserId, storedToken.CompanyId);
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

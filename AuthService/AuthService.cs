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
        _context = context;
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

            VendorUser user = new()
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
    public async Task<bool> CheckEmailRegisteredAsync(string phoneNumber)
    {
        phoneNumber = phoneNumber.GetCorrectPhoneNumber();

        return await _context.Users
            .AnyAsync(x => x.PhoneNumber == phoneNumber && !x.IsDeleted);
    }
    public async Task<object> SignAsync(SignDto dto)
    {
        var selector = await _context.Users
            .Where(x => x.Email == dto.Email && !x.IsDeleted)
            .Select(x => new
            {
                User = x,
            })
            .SingleOrDefaultAsync();

        var user = selector?.User;
        if (user is null)
            throw new NotFoundException("User_not_found_or_deleted");

        if (!user.IsValidPassword(dto.Password))
            throw new BadRequestException("Password_is_incorrect!");

        var companyIds = selector!.CompanyIds;

        var userId = user.Id;

        var deviceId = GetOrCreateDeviceToken();


        await SaveRefreshTokenAsync(userId, companyIds[0]);
        return await GenerateTokenAsync(userId, companyIds[0]);
    }
    public async Task<object> GetMeAsync(long userId, string? companyId)
    {
        var user = await _context.Users
                       .Where(x => x.Id == userId && !x.IsDeleted)
                       .Include(x => x.UserRole)
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
    private async Task<VendorUser> GetUser(long userId)
    {
        throw new NotImplementedException();
    }
    #endregion
}

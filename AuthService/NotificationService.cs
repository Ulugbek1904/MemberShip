using AuthService.Contracts.Auth;
using AuthService.Contracts.Auth.Notify;
using AuthService.Extensions;
using AuthService.Interfaces;
using Common.Common.Helpers;
using Common.Common.Models;
using Common.EF.Extensions;
using Common.Exceptions;
using Common.ResultWrapper.Library;
using Domain.Enum.Notify;
using Domain.Extensions;
using Domain.Models.Common;
using Infrastructure.Brokers.Notification.Push.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.SecurityTokenService;
using Serilog;
using System.Text.Json;

namespace AuthService;

public partial class AuthService : IAuthService
{
    #region SMS

    private static readonly string[] FIXED_MOBILE_PHONE_NUMBERS =
    {
        "998900324412",
        "998977391191",
        "998900021121"
    };
    private const string DEFAULT_OTP_CODE = "111111";

    public async Task<TokenResult> SendSmsAsync(SendSmsDto dto)
    {
        string normalizedPhone = dto.PhoneNumber.GetCorrectPhoneNumber();
        var now = DateTime.UtcNow;

        // Find existing valid OTPs
        var existingOtps = await _context.VerificationCodes
            .Where(x => x.PhoneNumber == normalizedPhone && !x.HasUsed && x.ExpiredDate > now)
            .OrderByDescending(x => x.ExpiredDate)
            .ToListAsync();

        if (existingOtps.Count != 0)
        {
            await ReuseValidOtpIfExistsAsync(existingOtps);
            return new TokenResult { SmsRequired = true };
        }

        bool isTestNumber = FIXED_MOBILE_PHONE_NUMBERS.Contains(normalizedPhone);
        string otpCode = isTestNumber ? DEFAULT_OTP_CODE : GenerateOtpCode();

        var verificationCode = await CreateOtpCodeAsync(normalizedPhone, otpCode);
        SetOtpCookie(verificationCode.Key, verificationCode.ExpiredDate);

        if (!string.IsNullOrWhiteSpace(dto.Hash))
            otpCode += $"\n{dto.Hash}";

        if (EnvironmentHelper.IsProduction && !isTestNumber)
        {
            try
            {
                await _iabsRpcService.SingleSendSmsAsync(new()
                {
                    PhoneNumber = normalizedPhone,
                    TemplateId = dto.TemplateId,
                    Variables = new() { { SmsMessages.OTP, otpCode } }
                });
            }
            catch (Exception ex)
            {
                await MarkVerificationCodesAsUsedAsync(normalizedPhone);
                throw new BadRequestException($"Failed to send SMS: {ex.Message}");
            }
        }

        return new TokenResult { SmsRequired = true };
    }
    public async Task VerifyOtpCodeAsync(VerifyOtpDto dto, bool skipCookie)
    {
        var verificationKey = _contextAccessor.HttpContext!
            .GetOrThrowExceptionCookie(_otpCookieKey);

        dto.Otp = EnvironmentHelper.IsProduction ? dto.Otp.Hash() : dto.Otp;
        var verificationCode = await _context.VerificationCodes
            .FirstOrDefaultAsync(x => x.Key == verificationKey && x.Otp == dto.Otp);

        if (verificationCode is null)
            throw new NotFoundException("Verification code is not found");

        if (verificationCode.HasUsed)
            throw new InvalidDataException("The verification code has already been used.");

        if (verificationCode.ExpiredDate < DateTime.UtcNow)
            throw new InvalidDataException("The verification code has expired.");

        verificationCode.HasUsed = true;
        _context.VerificationCodes.Update(verificationCode);
        await _context.SaveChangesAsync();

        if (!skipCookie)
        {
            _contextAccessor.HttpContext!.SetCookie(
                _contactVerifiedCookieKey,
                "true",
                DateTime.UtcNow.AddMinutes(_registrationExpirationInMinutes));
        }
    }
    private async Task ReuseValidOtpIfExistsAsync(List<VerificationCode> existingOtps)
    {
        var latestOtp = existingOtps.First();
        var redundantOtps = existingOtps.Skip(1).Select(x => x.Id).ToList();

        if (redundantOtps.Count != 0)
        {
            await _context.VerificationCodes
                .Where(x => redundantOtps.Contains(x.Id))
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(x => x.HasUsed, true));
        }

        SetOtpCookie(latestOtp.Key, latestOtp.ExpiredDate);
    }
    private async Task<VerificationCode> CreateOtpCodeAsync(string phoneNumber, string otpCode)
    {
        VerificationCode? verificationCode;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await MarkVerificationCodesAsUsedAsync(phoneNumber);

            verificationCode = new()
            {
                PhoneNumber = phoneNumber,
                HasUsed = false,
                Otp = otpCode.Hash(),
                ExpiredDate = DateTime.UtcNow.AddMinutes(_otpExpirationInMinutes),
                Key = Guid.NewGuid().ToString()
            };

            _context.VerificationCodes.Add(verificationCode);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SendSmsAsync failed for {PhoneNumber}", phoneNumber);
            await transaction.RollbackAsync();
            throw;
        }

        return verificationCode;
    }
    private void SetOtpCookie(string key, DateTime expiration)
    {
        _contextAccessor.HttpContext!.SetCookie(
            _otpCookieKey,
            key,
            expiration);
    }
    private async Task MarkVerificationCodesAsUsedAsync(string phoneNumber)
    {
        await _context.VerificationCodes
            .Where(vc => vc.PhoneNumber == phoneNumber && !vc.HasUsed)
            .ExecuteUpdateAsync(vc =>
                vc.SetProperty(v => v.HasUsed, true));
    }
    private static string GenerateOtpCode()
    {
        string otpCode = EnvironmentHelper.IsProduction
            ? PasswordHelper.GenerateRandom6DigitNumber().ToString()
            : DEFAULT_OTP_CODE;

        return otpCode;
    }

    #endregion

    #region PUSH
    public async Task<Wrapper> GetSourcesAsync(DataQueryRequest request)
    {
        return await _context.PushNotificationSources
            .Where(x => !x.IsDeleted)
            .GetByDataQueryAsync(request);
    }
    public async Task<Wrapper> GetSourceAsync(long id)
    {
        return (content: await _context.PushNotificationSources.GetByIdOrThrowsNotFoundException(id), 200);
    }
    public async Task<long> CreateSourceAsync(PushNotificationSource source)
    {
        source.Id = 0;
        source.ThumbUrl = $"{_fileConfig.SourceUrl}{source.ThumbUrl}";
        var entity = _context.PushNotificationSources.Add(source).Entity;
        await _context.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<long> UpdateSourceAsync(PushNotificationSource source)
    {
        var entity = await _context.PushNotificationSources
            .GetByIdOrThrowsNotFoundException(source.Id);

        entity.Title = source.Title;
        entity.Description = source.Description;
        entity.ThumbUrl = source.ThumbUrl;

        await _context.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<long> DeleteSourceAsync(long id)
    {
        var entity = await _context.PushNotificationSources
            .GetByIdOrThrowsNotFoundException(id);

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task PushToOneUserAsync(PushNotifyToOneUserDto dto)
    {
        var user = await _context.Users
            .Where(u => u.Id == dto.UserId)
            .Select(u => new { u.Id, u.CanNotReceivePushNotifications })
            .SingleOrDefaultAsync();

        if (user == null || user.CanNotReceivePushNotifications)
            return;

        PushNotification notification;
        PushNotificationRequest request;
        if (dto.SourceId.HasValue)
        {
            var source = await _context.PushNotificationSources
                             .Where(s => s.Id == dto.SourceId)
                             .Select(s => new
                             {
                                 s.Id,
                                 s.Title,
                                 s.Description,
                                 s.ThumbUrl
                             })
                             .SingleOrDefaultAsync()
                         ?? throw new NotFoundException("Push_notification_sources_not_found");

            notification = new()
            {
                UserId = user.Id,
                SourceId = source.Id,
                IsRead = false
            };

            request = new()
            {
                Title = source.Title,
                Description = source.Description,
                ThumbUrl = source.ThumbUrl
            };
        }
        else
        {
            notification = new()
            {
                UserId = user.Id,
                Title = dto.Title,
                Description = dto.Description,
                ThumbUrl = $"{_fileConfig.SourceUrl}{dto.ThumbUrl}",
                IsRead = false
            };

            request = new()
            {
                Title = dto.Title,
                Description = dto.Description,
                ThumbUrl = notification.ThumbUrl
            };
        }
        await _notifyBroker.Push(request, new[] { user.Id });

        _context.PushNotifications.Add(notification);
        await _context.SaveChangesAsync();
    }
    public async Task PushToAllUsersAsync(PushNotifyToAllUsersDto dto)
    {
        var query = _context.UserToCompanies
            .Where(x => !x.User.CanNotReceivePushNotifications
                        && !x.User.IsDeleted
                        && x.Company.IsMobile
                        && !x.Company.IsDeleted);

        if (dto.Filter != null)
        {
            if (dto.Filter.CompanyId.HasValue)
                query = query.Where(x => x.CompanyId == dto.Filter.CompanyId.Value);

            if (dto.Filter.StatusId.HasValue)
                query = query.Where(x => x.User.StatusId == dto.Filter.StatusId.Value);

            if (dto.Filter.IsDirector.HasValue)
                query = query.Where(x => x.User.IsDirector == dto.Filter.IsDirector.Value);

            if (dto.Filter.RegionId.HasValue)
                query = query.Where(x => x.Company.RegionId == dto.Filter.RegionId.Value);

            if (dto.Filter.DistrictId.HasValue)
                query = query.Where(x => x.Company.DistrictId == dto.Filter.DistrictId.Value);

            if (dto.Filter.TypeClientId.HasValue)
                query = query.Where(x => x.Company.TypeClientId == dto.Filter.TypeClientId.Value);

            if (dto.Filter.BankFilialId.HasValue)
                query = query.Where(x => x.Company.BankFilialId == dto.Filter.BankFilialId.Value);
        }
        var utc = await query.ToListAsync();

        PushNotificationRequest request;
        if (dto.SourceId.HasValue)
        {
            var source = await _context.PushNotificationSources
                             .Where(x => x.Id == dto.SourceId.Value)
                             .Select(x => new
                             {
                                 x.Id,
                                 x.Description,
                                 x.ThumbUrl,
                                 x.Title
                             })
                             .SingleOrDefaultAsync()
                         ?? throw new NotFoundException("Push_notification_sources_not_found");

            request = new()
            {
                Title = source.Title,
                Description = source.Description,
                ThumbUrl = source.ThumbUrl,
                SourceId = source.Id
            };
        }
        else
        {
            request = new()
            {
                Title = dto.Title,
                Description = dto.Description,
                ThumbUrl = $"{_fileConfig.SourceUrl}{dto.ThumbUrl}"
            };
        }

        var userIds = utc.Select(x => x.UserId).ToArray();
        queue.Enqueue(request, userIds);
    }
    public async Task<Wrapper> GetMyNotificationsAsync(DataQueryRequest request, long userId)
    {
        return await _context.PushNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.IsRead,
                x.CreatedAt,
                x.SourceId,
                Title = x.SourceId != null ? x.Source!.Title : x.Title,
                Description = x.SourceId != null ? x.Source!.Description : x.Description,
                ThumbUrl = x.SourceId != null ? x.Source!.ThumbUrl : x.ThumbUrl
            })
            .GetByDataQueryAsync(request);
    }
    public async Task SelfPreferenceNotificationAsync(long userId, bool canNotReceivePushNotifications)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                   ?? throw new NotFoundException($"User_was_not_found");

        if (user.CanNotReceivePushNotifications == canNotReceivePushNotifications)
            return;

        user.CanNotReceivePushNotifications = canNotReceivePushNotifications;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<object> SetAsReadAsync(long pushNotificationId, long userId)
    {
        var notification = await _context.PushNotifications
                               .Where(x => x.Id == pushNotificationId && x.UserId == userId)
                               .SingleOrDefaultAsync()
                           ?? throw new NotFoundException();

        notification!.IsRead = true;
        await _context.SaveChangesAsync();
        return notification;
    }
    public async Task SetAllAsReadAsync(long userId)
    {
        var pushNotifications = await _context.PushNotifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var pushNotification in pushNotifications)
            pushNotification!.IsRead = true;

        await _context.SaveChangesAsync();
    }
    #endregion
}
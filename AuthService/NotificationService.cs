using AuthService.Contracts.Auth;
using AuthService.Extensions;
using AuthService.Interfaces;
using Common.Common.Helpers;
using Common.Common.Models;
using Common.Exceptions;
using Common.ResultWrapper.Library;
using Domain.Extensions;
using Domain.Models.Common;
using Infrastructure.Brokers.Notification.Push;
using Infrastructure.Brokers.Notification.Push.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.SecurityTokenService;
using Serilog;

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
    public async Task<Wrapper> GetSourceAsync(EnumPushNotificationSource source)
    {
        return (content: await _context.PushNotificationSources
            .FirstOrDefaultAsync(x => x.Source == source), 200);
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

    #region PopUp 
    public async Task<Wrapper> GetPopUpSourcesAsync(DataQueryRequest request)
    {
        return await _context.PopUpNotificationSources
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                TypeId = (int)x.Type,
                TypeName = x.Type.ToString(),
                TriggerId = (int)x.Trigger,
                TriggerName = x.Trigger.ToString(),
                x.ThumbUrl,
                x.DisplayLimitPerUser
            })
            .GetByDataQueryAsync(request);
    }
    public async Task<Wrapper> GetPopUpSourceAsync(long id)
    {
        return (content: await _context
            .PopUpNotificationSources.GetByIdOrThrowsNotFoundException(id), 200);
    }
    public async Task<long> CreatePopUpSourceAsync(PopUpNotificationSource source)
    {
        source.Id = 0;
        var entity = _context.PopUpNotificationSources.Add(source).Entity;
        await _context.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<long> UpdatePopUpSourceAsync(PopUpNotificationSource source)
    {
        var entity = await _context.PopUpNotificationSources
            .GetByIdOrThrowsNotFoundException(source.Id);

        entity.Title = source.Title;
        entity.Description = source.Description;
        entity.ThumbUrl = source.ThumbUrl;
        entity.Type = source.Type;
        entity.Trigger = source.Trigger;
        entity.TriggerValue = source.TriggerValue;
        entity.DisplayLimitPerUser = source.DisplayLimitPerUser;

        await _context.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<long> DeletePopUpSourceAsync(long id)
    {
        var entity = await _context.PopUpNotificationSources
            .GetByIdOrThrowsNotFoundException(id);
        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<PopUpMetricsDto> GetMetricsAsync(MetriksRequest dto)
    {
        var query = _context.PopUpNotifications.Where(x => x.SourceId == dto.SourceId);

        if (dto.FromDate.HasValue) query = query.Where(x => x.CreatedAt >= dto.FromDate.Value);
        if (dto.ToDate.HasValue) query = query.Where(x => x.CreatedAt <= dto.ToDate.Value);

        var impressions = await query.CountAsync(x => x.IsShown);
        var clicks = await query.CountAsync(x => x.IsClicked);
        var closes = await query.CountAsync(x => x.IsClosed);
        var submissions = await query.CountAsync(x => x.EventDataJson != null);



        return new PopUpMetricsDto
        {
            SourceId = dto.SourceId,
            Impressions = impressions,
            Clicks = clicks,
            Closes = closes,
            FormSubmissions = submissions
        };
    }

    public async Task<long> CreatePopUpForUserAsync(PopUpNotifyDto dto)
    {
        var notification = new PopUpNotification
        {
            UserId = dto.UserId,
            SourceId = dto.SourceId,
            Title = dto.Title,
            Description = dto.Description,
            ThumbUrl = dto.ThumbUrl,
            Type = dto.Type,
            IsShown = true,
            IsClicked = false,
            IsClosed = false
        };
        _context.PopUpNotifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification.Id;
    }
    public async Task<List<PopUpNotification>> GetUserNotificationsAsync(long userId)
    {
        return await _context.PopUpNotifications
            .Where(x => x.UserId == userId)
            .Include(x => x.Source)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
    public async Task<object> MarkAsShownAsync(long sourceId, long userId)
    {
        var source = await _context.PopUpNotificationSources.
            Where(s => !s.IsDeleted)
            .GetByIdOrThrowsNotFoundException(sourceId);

        var notification = new PopUpNotification
        {
            UserId = userId,
            SourceId = sourceId,
            Title = source.Title,
            Description = source.Description,
            ThumbUrl = source.ThumbUrl,
            Type = source.Type,
            IsShown = true,
            ShownAt = DateTime.Now,
            IsClicked = false,
            IsClosed = false
        };

        _context.PopUpNotifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification.Id;
    }
    public async Task MarkAsClickedAsync(long id)
    {
        var entity = await _context.PopUpNotifications
            .GetByIdOrThrowsNotFoundException(id);

        entity.IsClicked = true;
        await _context.SaveChangesAsync();
    }
    public async Task MarkAsClosedAsync(long id)
    {
        var entity = await _context.PopUpNotifications
            .GetByIdOrThrowsNotFoundException(id);

        entity.IsClosed = true;
        await _context.SaveChangesAsync();
    }
    public async Task<IEnumerable<ActivePopupDto>> GetActivePopupsAsync(long userId, ActivePopUpRequestDto dto)
    {
        var now = DateTime.UtcNow.Date;

        var query = _context.PopUpNotificationSources
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (dto.IsOnRoute == true)
        {
            query = query.Where(p => p.Trigger == EnumPopUpTrigger.OnRoute);
        }
        else if (dto.IsOnRoute == false)
        {
            query = query.Where(p => p.Trigger == EnumPopUpTrigger.OnTime);
        }


        var candidates = await query.ToListAsync();

        if (candidates.Count == 0)
            return Enumerable.Empty<ActivePopupDto>();

        var shownCounts = await _context.PopUpNotifications
            .Where(e => e.UserId == userId && e.IsShown && e.ShownAt >= now && !e.IsDepositOffer)
            .GroupBy(e => e.SourceId)
            .Select(g => new { SourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => (long)x.SourceId!, x => x.Count);

        var allowed = new List<ActivePopupDto>();

        foreach (var p in candidates)
        {
            var shownCount = shownCounts.TryGetValue(p.Id, out var count) ? count : 0;
            if (shownCount >= p.DisplayLimitPerUser)
                continue;

            allowed.Add(new ActivePopupDto
            {
                SourceId = p.Id,
                Title = p.Title,
                Description = p.Description,
                ThumbUrl = p.ThumbUrl,
                Type = p.Type,
                Trigger = p.Trigger,
                TriggerPayload = p.TriggerValue ?? null,
                DisplayLimitPerUser = p.DisplayLimitPerUser
            });
        }

        return allowed;
    }
    public async Task RecordEventAsync(PopupEventDto dto)
    {
        var src = await _context.PopUpNotificationSources
            .Where(x => !x.IsDeleted)
            .GetByIdOrThrowsNotFoundException(dto.SourceId);

        var existing = await _context.PopUpNotifications
            .Where(x => x.SourceId == dto.SourceId && x.UserId == dto.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            existing = new PopUpNotification
            {
                SourceId = dto.SourceId,
                UserId = dto.UserId,
                Title = src.Title.Uz,
                Description = src.Description?.Uz,
                ThumbUrl = src.ThumbUrl,
                Type = src.Type
            };
            _context.PopUpNotifications.Add(existing);
        }

        var now = DateTime.UtcNow;

        switch (dto.EventType)
        {
            case "shown":
                existing.IsShown = true;
                existing.ShownAt = now;
                break;
            case "clicked":
                existing.IsClicked = true;
                break;
            case "closed":
                existing.IsClosed = true;
                break;
            case "form_submitted":
                existing.EventDataJson = dto.EventData == null
                    ? null
                    : JsonSerializer.Serialize(dto.EventData);
                break;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<PopUpNotification?> GetWeeklyDepositOfferAsync(long companyId, long userId)
    {
        var today = DateTime.UtcNow;
        var currentWeekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
        var nextWeekStart = currentWeekStart.AddDays(7);

        bool popupExistsThisWeek = await _context.PopUpNotifications
            .AnyAsync(x =>
                x.UserId == userId &&
                x.IsDepositOffer &&
                x.CreatedAt >= currentWeekStart &&
                x.CreatedAt < nextWeekStart);

        if (popupExistsThisWeek)
        {
            return null;
        }

        var popup = await GetDepositOffersForCompanyAsync(companyId, userId);

        if (popup != null)
        {
            _context.PopUpNotifications.Add((PopUpNotification)popup);
            await _context.SaveChangesAsync();
            return (PopUpNotification)popup;
        }

        return null;
    }



    private async Task<object> GetDepositOffersForCompanyAsync(long companyId, long userId)
    {
        var depositProducts = await _context.DpProducts
            .Where(p => !p.IsDeleted && p.State == "A")
            .Select(p => new
            {
                p.Id,
                p.ProductName,
                p.MinSum,
                p.MaxSum,
                p.PercRate,
                Percentages = p.Percentages
                    .Select(per => new
                    {
                        per.PercRate,
                        per.BeginDay,
                        per.EndDay
                    })
                    .ToList()
            })
            .ToListAsync();

        var company = await _context.Companies
            .Where(c => c.Id == companyId && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.MainAccount
            })
            .FirstOrDefaultAsync();

        double minBalance = await FindMinimumBalanceForCompany(companyId, company?.MainAccount!);

        if (minBalance <= 0)
        {
            return null;
        }

        var suitableProducts = depositProducts
            .Where(p => minBalance >= p.MinSum && minBalance <= p.MaxSum)
            .ToList();

        if (!suitableProducts.Any())
        {
            return null;
        }

        var offers = suitableProducts.Select(p =>
        {
            double maxRate = p.Percentages.Any()
                ? p.Percentages.Max(x => x.PercRate)
                : (p.PercRate ?? 0);

            double estimatedIncome = minBalance * maxRate / 100;

            return new
            {
                p.ProductName,
                p.MinSum,
                p.MaxSum,
                MaxPercentage = maxRate,
                EstimatedIncome = Math.Round(estimatedIncome, 2)
            };
        }).ToList();

        var bestOffer = offers.OrderByDescending(x => x.MaxPercentage).First();

        return new PopUpNotification
        {
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            Type = EnumPopUpNotificationType.Information,
            IsDepositOffer = true,
            SourceId = null,
            Description = $"{bestOffer.ProductName} " +
                     $"depoziti {bestOffer.MaxPercentage}% stavkada. " +
                     $"1 yilda taxminan {bestOffer.EstimatedIncome:N0} so‘m daromad keltiradi.",
            Title = $"Maxsus taklif: {company?.Name} uchun depozit",
            IsShown = true,
            ShownAt = DateTime.UtcNow,
            ThumbUrl = null
        };
    }

    private async Task<double> FindMinimumBalanceForCompany(long companyId, string mainAccount)
    {
        int periodDays = 14;
        var fromDate = DateTime.UtcNow.AddDays(-periodDays).ToString("dd.MM.yyyy");
        var toDate = DateTime.UtcNow.ToString("dd.MM.yyyy");

        string accountNumber = mainAccount;

        if (string.IsNullOrEmpty(accountNumber))
            accountNumber = await GetActiveAcountNumberByCompanyIdAsync(companyId);

        if (string.IsNullOrEmpty(accountNumber))
            return 0;

        var request = new ReportBaseRequestWithPagination
        {
            Page = 1,
            Size = 2000,
            AccountNumber = accountNumber,
            FromDate = fromDate,
            ToDate = toDate
        };

        var turnoverResponse = await _iabsRpcService
            .TurnoverReceiptAccountByDocument(request);

        if (turnoverResponse?.Data == null || !turnoverResponse.Data.Any())
            return 0;

        var dailyBalances = turnoverResponse.Data
            .GroupBy(x => x.OperationDate)
            .Select(g =>
            {
                var first = g.First();
                double.TryParse(first.DayEndBalance?.ToString(), out double balance);
                return balance;
            })
            .ToList();

        return dailyBalances.Any() ? dailyBalances.Min() : 0;
    }

    private Task<string> GetActiveAcountNumberByCompanyIdAsync(long companyId)
    {
        const string currencyCode = "000";

        var accountNumber = _context.ActiveAccounts
            .Where(a => a.CompanyId == companyId && a.CurrencyCode == currencyCode && a.AccountCode == "22800")
            .Select(a => a.AccountNumber)
            .FirstOrDefault();

        return Task.FromResult(accountNumber ?? string.Empty);
    }
    #endregion
}

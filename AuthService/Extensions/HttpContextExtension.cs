using Common.Common.Helpers;
using Common.Exceptions.Common;
using Microsoft.AspNetCore.Http;

namespace AuthService.Extensions;

public static class HttpContextExtension
{
    public static void SetCookie(this HttpContext context, string key, string value, DateTime expiration)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = EnvironmentHelper.IsProduction,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiration
        };

        context.Response.Cookies.Append(key, value, cookieOptions);
    }
    public static string GetOrThrowExceptionCookie(this HttpContext context, string key)
    {
        if (!context.Request.Cookies.TryGetValue(key, out var value))
        {
            throw new UnauthorizedException();
        }

        return value;
    }
    public static string GetCookie(this HttpContext context, string key)
    {
        if (!context.Request.Cookies.TryGetValue(key, out var value))
        {
            return "";
        }

        return value;
    }
}

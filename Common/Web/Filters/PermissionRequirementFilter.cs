using Common.Common.Extensions;
using Common.Exceptions.Common;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Common.Web.Filters;

public class PermissionRequirementFilter : IAsyncAuthorizationFilter, IFilterMetadata
{
    private readonly int[] _requiredPermissionsCodes;

    public const string PermissionClaimKey = "permissions";

    public PermissionRequirementFilter(int[] requiredPermissionsCodes)
    {
        _requiredPermissionsCodes = requiredPermissionsCodes;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        string text = context.HttpContext.User.FindFirstValue("permissions");
        if (text.IsNullOrEmpty())
        {
            throw new UnauthorizedException();
        }

        if (!text.IsNullOrEmpty())
        {
            IEnumerable<int> permissionCodes = null;
            try
            {
                permissionCodes = text.Split(", ").Select(int.Parse);
            }
            catch (Exception exception)
            {
                throw new UnauthorizedException(exception);
            }

            if (_requiredPermissionsCodes.Any((int x) => permissionCodes.All((int pc) => pc != x)))
            {
                throw new ForbiddenException("Forbidden");
            }

            return Task.CompletedTask;
        }

        throw new UnauthorizedException();
    }
}

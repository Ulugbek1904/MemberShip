using Common.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Common.Web.Attributes;

public class AuthorizeAttribute : TypeFilterAttribute
{
    public AuthorizeAttribute(params int[] permissions)
        : base(typeof(PermissionRequirementFilter))
    {
        base.Arguments = new object[1] { permissions };
    }
}

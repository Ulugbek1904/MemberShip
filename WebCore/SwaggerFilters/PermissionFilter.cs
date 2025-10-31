

using Common.Common.Helpers;
using Common.Web.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace WebCore.SwaggerFilters;

public class PermissionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorizeAttribute = context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>();

        if (authorizeAttribute is not null)
        {
            operation.Description += $"REQUIRED PERMISSION CODE(s): {(!authorizeAttribute.Arguments.IsNullOrEmpty() ? SerializerHelper.ToJsonString(authorizeAttribute.Arguments![0]) : "[NONE]")}";
        }
    }
}

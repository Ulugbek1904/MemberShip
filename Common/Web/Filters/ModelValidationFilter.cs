using Common.ResultWrapper.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel.DataAnnotations;

namespace Common.Web.Filters;

public class ModelValidationFilter : IAsyncActionFilter, IFilterMetadata
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ModelState.IsValid)
        {
            await next();
            return;
        }

        context.HttpContext.Response.StatusCode = 400;
        context.Result = new ObjectResult(WrapperGeneric<object>.ResultFromModelState(context.ModelState, new ValidationException()));
    }
}
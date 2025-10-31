
using Common.Exceptions.Common;
using Common.ResultWrapper.Library;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;
using System.Net;

namespace Common.Web.Middlewares;

public class GlobalExceptionHandlerMiddleware(bool trackBody = false) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Activity.Current != null)
        {
            Activity current = Activity.Current;
            context.Response.Headers.Append("trace-id", current.TraceId.ToString());
            context.Response.Headers.TraceParent = current.Id;
        }

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (ex is ApiException ex2)
            {
                context.Response.StatusCode = ex2.StatusCode;
                await context.Response.WriteAsJsonAsync(WrapperGeneric<object>.ResultFromException(ex, (HttpStatusCode)ex2.StatusCode));
                return;
            }

            if (Activity.Current != null)
            {
                Activity.Current.SetStatus(ActivityStatusCode.Error, ex.Message);
                Activity.Current.AddException(ex, default(TagList));
                if (trackBody)
                {
                    Activity.Current.SetTag("http.request.body", context.Request.Body.ToString());
                }
            }

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(WrapperGeneric<object>.ResultFromException(ex));
            Log.Error("TraceId: {0} Exception: \n{1}", Activity.Current?.TraceId, ex);
        }
    }
}

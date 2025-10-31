using Common.ResultWrapper.Library.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;
using System.Text.Json.Serialization;

namespace Common.ResultWrapper.Library;

public class WrapperGeneric<T> : IWrapperGeneric<T>
{
    [JsonInclude]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("code")]
    public HttpStatusCode Code { get; init; } = HttpStatusCode.OK;

    [JsonPropertyName("content")]
    public T? Content { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    public object? Query { get; set; }

    [JsonPropertyName("modelStateError")]
    public List<Common.ModelError>? ModelStateError { get; init; }

    [JsonIgnore]
    public string? StackTrace { get; init; }

    public WrapperGeneric()
    {
    }

    public WrapperGeneric(Exception? exception, HttpStatusCode code = HttpStatusCode.InternalServerError)
    {
        Code = code;
        Error = exception?.Message;
        StackTrace = exception?.StackTrace;
    }

    public WrapperGeneric(T? content, HttpStatusCode code = HttpStatusCode.OK)
    {
        Content = content;
        Code = code;
    }

    public WrapperGeneric(HttpStatusCode code)
    {
        Code = code;
    }

    public WrapperGeneric(ModelStateDictionary modelState, Exception? exception = null)
    {
        Code = HttpStatusCode.BadRequest;
        ModelStateError = (from x in modelState.Where<KeyValuePair<string, ModelStateEntry>>(delegate (KeyValuePair<string, ModelStateEntry> x)
        {
            ModelStateEntry value = x.Value;
            return value != null && value.ValidationState == ModelValidationState.Invalid;
        })
                           select new ResultWrapper.Library.Common.ModelError
                           {
                               Key = x.Key,
                               ErrorMessage = x.Value?.Errors.FirstOrDefault()?.ErrorMessage
                           }).ToList();
        WrapperGeneric<T> wrapperGeneric = new WrapperGeneric<T>(exception);
        Error = wrapperGeneric.Error;
        StackTrace = wrapperGeneric.StackTrace;
    }

    public static WrapperGeneric<T> ResultFromException(Exception exception, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new WrapperGeneric<T>(exception, statusCode);
    }

    public static IWrapperGeneric<T> ResultFromModelState(ModelStateDictionary modelState, Exception? exception = null)
    {
        return new WrapperGeneric<T>(modelState, exception);
    }

    public static WrapperGeneric<T> ResultFromContent(T content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new WrapperGeneric<T>(content, statusCode);
    }

    public static implicit operator WrapperGeneric<T>(Exception exception)
    {
        return new WrapperGeneric<T>(exception);
    }

    public static implicit operator WrapperGeneric<T>((Exception exception, int statusCode) data)
    {
        return new WrapperGeneric<T>(data.exception, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator WrapperGeneric<T>((T? content, int statusCode) data)
    {
        return new WrapperGeneric<T>(data.content, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator WrapperGeneric<T>(int statusCode)
    {
        return new WrapperGeneric<T>((HttpStatusCode)statusCode);
    }
}

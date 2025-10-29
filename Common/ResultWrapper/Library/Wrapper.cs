using Common.ResultWrapper.Library.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace Common.ResultWrapper.Library;

public class Wrapper : WrapperGeneric<object>, IWrapper, IWrapperGeneric<object>
{
    public Wrapper()
    {
    }

    public Wrapper(Exception? exception, HttpStatusCode code = HttpStatusCode.InternalServerError)
        : base(exception, code)
    {
    }

    public Wrapper(object? content, HttpStatusCode code = HttpStatusCode.OK)
        : base(content, code)
    {
    }

    public Wrapper(HttpStatusCode code)
        : base(code)
    {
    }

    public Wrapper(ModelStateDictionary modelState, Exception? exception = null)
        : base(modelState, exception)
    {
    }

    public Wrapper(IEnumerable<object> content, int total, HttpStatusCode code = HttpStatusCode.OK)
        : base(code)
    {
        base.Content = content;
        base.Code = code;
        base.Total = total;
    }

    public Wrapper(IEnumerable<object> content, int total, object? query, HttpStatusCode code = HttpStatusCode.OK)
        : base(code)
    {
        base.Content = content;
        base.Code = code;
        base.Total = total;
        base.Query = query;
    }

    public Wrapper(string message, HttpStatusCode code = HttpStatusCode.OK)
        : base((object?)message, code)
    {
        base.Content = message;
        base.Code = code;
    }

    public static implicit operator Wrapper(string s)
    {
        return new Wrapper(s);
    }

    public static Wrapper FromIConvertible(IConvertible s)
    {
        return new Wrapper(s);
    }

    public static implicit operator Wrapper((string content, int statusCode) data)
    {
        return new Wrapper(data.content, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator Wrapper((IConvertible content, int statusCode) data)
    {
        return new Wrapper(data.content, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator Wrapper((IEnumerable<object> items, int total) data)
    {
        return new Wrapper(data.items, data.total);
    }

    public static implicit operator Wrapper((IEnumerable<object> items, int total, object? query) data)
    {
        return new Wrapper(data.items, data.total, data.query);
    }

    public static implicit operator Wrapper((IEnumerable<IComparable> items, int total) data)
    {
        return new Wrapper(data.items, data.total);
    }

    public static implicit operator Wrapper((IEnumerable<IComparable> items, int total, object? query) data)
    {
        return new Wrapper(data.items, data.total, data.query);
    }

    public static implicit operator Wrapper((IEnumerable<object> items, int total, int statusCode) data)
    {
        return new Wrapper(data.items, data.total, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator Wrapper((IEnumerable<IComparable> items, int total, int statusCode) data)
    {
        return new Wrapper(data.items, data.total, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator Wrapper(Exception exception)
    {
        return new Wrapper(exception);
    }

    public static implicit operator Wrapper((Exception exception, int statusCode) data)
    {
        return new Wrapper(data.exception, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator Wrapper((object? content, int statusCode) data)
    {
        return new Wrapper(data.content, (HttpStatusCode)data.statusCode);
    }

    public static implicit operator Wrapper(int statusCode)
    {
        return new Wrapper((HttpStatusCode)statusCode);
    }
}

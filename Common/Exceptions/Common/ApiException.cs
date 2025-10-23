namespace Common.Exceptions.Common;

public class ApiException : Exception
{
    public virtual int StatusCode { get; set; }

    protected ApiException(string message)
        : base(message)
    {
    }

    protected ApiException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected ApiException(Exception exception)
        : base(exception.Message, exception)
    {
        StatusCode = 500;
    }

    protected ApiException()
    {
    }
}

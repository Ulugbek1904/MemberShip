namespace Common.Exceptions.Common;

public class UnauthorizedException : ApiException
{
    public override int StatusCode => 401;

    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException()
        : base("Unauthorized")
    {
    }

    public UnauthorizedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public UnauthorizedException(Exception exception)
        : base(exception)
    {
    }
}

namespace Common.Exceptions.Common;

public sealed class AlreadyExistsException : ApiException
{
    public override int StatusCode => 403;

    public AlreadyExistsException(string message)
        : base(message)
    {
    }

    public AlreadyExistsException()
    {
    }

    public AlreadyExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

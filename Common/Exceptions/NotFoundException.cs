using Common.Exceptions.Common;

namespace Common.Exceptions;

public sealed class NotFoundException : ApiException
{
    public override int StatusCode => 404;

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException()
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static void ThrowIfNull(object? data, string message = "Not found")
    {
        if (data == null)
        {
            throw new NotFoundException(message);
        }
    }
}
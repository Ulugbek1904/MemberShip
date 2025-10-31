using Common.Exceptions.Common;

namespace Common.Common.Extensions;

public sealed class ForbiddenException : ApiException
{
    public override int StatusCode => 403;

    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException()
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

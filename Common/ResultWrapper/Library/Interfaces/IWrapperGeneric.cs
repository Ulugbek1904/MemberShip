using Common.ResultWrapper.Library.Common;
using System.Net;

namespace Common.ResultWrapper.Library.Interfaces;

public interface IWrapperGeneric<T>
{
    Guid Id { get; set; }

    HttpStatusCode Code { get; init; }

    T? Content { get; init; }

    string? Error { get; init; }

    int? Total { get; set; }

    object? Query { get; set; }

    List<ModelError>? ModelStateError { get; init; }

    string? StackTrace { get; init; }
}

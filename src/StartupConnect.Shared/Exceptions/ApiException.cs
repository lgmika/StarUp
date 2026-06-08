using System.Net;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Shared.Exceptions;

public class ApiException : Exception
{
    public ApiException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        IReadOnlyCollection<ErrorDetail>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? Array.Empty<ErrorDetail>();
    }

    public HttpStatusCode StatusCode { get; }

    public IReadOnlyCollection<ErrorDetail> Errors { get; }
}


using System.Net;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Shared.Exceptions;

public sealed class ValidationException : ApiException
{
    public ValidationException(IReadOnlyCollection<ErrorDetail> errors)
        : base("Validation failed", HttpStatusCode.BadRequest, errors)
    {
    }
}


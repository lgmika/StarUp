namespace StartupConnect.Shared.Responses;

public sealed record ErrorResponse(
    bool Success,
    string Message,
    IReadOnlyCollection<ErrorDetail> Errors)
{
    public static ErrorResponse Fail(string message, IReadOnlyCollection<ErrorDetail>? errors = null)
    {
        return new ErrorResponse(false, message, errors ?? Array.Empty<ErrorDetail>());
    }
}


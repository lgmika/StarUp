namespace StartupConnect.Shared.Responses;

public sealed record ErrorDetail(string Code, string Message, string? Field = null);


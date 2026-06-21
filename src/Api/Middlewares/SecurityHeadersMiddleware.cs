using Microsoft.Extensions.Options;
using StartupConnect.Api.Security;

namespace StartupConnect.Api.Middlewares;

public sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IOptions<StartupConnectSecurityOptions> options)
{
    private readonly StartupConnectSecurityOptions _options = options.Value;

    public Task InvokeAsync(HttpContext context)
    {
        if (_options.EnableSecurityHeaders)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                // Swagger UI is only exposed in Development and needs its own scripts/styles.
                if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
                {
                    headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
                }
                headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}

using System.Diagnostics;
using Microsoft.Extensions.Options;
using StartupConnect.Api.Observability;

namespace StartupConnect.Api.Middlewares;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    IOptions<ObservabilityOptions> optionsAccessor)
{
    private readonly ObservabilityOptions options = optionsAccessor.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.EnableRequestLogging)
        {
            await next(context);
            return;
        }

        var correlationId = GetOrCreateCorrelationId(context);
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        context.Response.Headers[options.CorrelationHeaderName] = correlationId;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var logLevel = elapsedMs >= options.SlowRequestThresholdMs || context.Response.StatusCode >= 500
                ? LogLevel.Warning
                : LogLevel.Information;

            logger.Log(
                logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0} ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                elapsedMs);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(options.CorrelationHeaderName, out var values) &&
            !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return NormalizeCorrelationId(values.First(), context.TraceIdentifier);
        }

        return context.TraceIdentifier;
    }

    public static string NormalizeCorrelationId(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return fallback;
        }

        return value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
            ? value
            : fallback;
    }
}

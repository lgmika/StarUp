using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Api.Extensions;

public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteStartupConnectHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object>.Ok(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                error = entry.Value.Exception is null ? null : "Health check failed"
            })
        });

        return JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
    }
}

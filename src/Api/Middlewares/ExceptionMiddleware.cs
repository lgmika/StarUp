using System.Net;
using System.Text.Json;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Api.Middlewares;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(context, exception.StatusCode, exception.Message, exception.Errors);
        }
        catch (BadHttpRequestException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(
                context,
                HttpStatusCode.BadRequest,
                "Invalid request",
                [new ErrorDetail("InvalidRequest", "The request body or parameters are invalid", null)]);
        }
        catch (JsonException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(
                context,
                HttpStatusCode.BadRequest,
                "Invalid JSON payload",
                [new ErrorDetail("InvalidJson", "The request body contains invalid JSON", null)]);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request was cancelled by the client: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteErrorAsync(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred",
                Array.Empty<ErrorDetail>());
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        IReadOnlyCollection<ErrorDetail> errors)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ErrorResponse.Fail(message, errors);
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions);
    }
}

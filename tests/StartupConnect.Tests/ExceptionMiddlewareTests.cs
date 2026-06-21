using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using StartupConnect.Api.Middlewares;
using StartupConnect.Api.Extensions;
using StartupConnect.Shared.Exceptions;
using System.Text;

namespace StartupConnect.Tests;

public sealed class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Return_400_For_Invalid_Request_Body()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new BadHttpRequestException("Malformed JSON"),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("InvalidRequest", body);
        Assert.DoesNotContain("Malformed JSON", body);
    }

    [Fact]
    public async Task InvokeAsync_Should_Rethrow_When_Response_Has_Started()
    {
        var expected = new InvalidOperationException("late failure");
        var middleware = new GlobalExceptionMiddleware(
            _ => throw expected,
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task ReadBoundedBodyAsync_Should_Reject_Chunked_Oversized_Payload()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("1234"));
        context.Request.ContentLength = null;

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            EndpointRouteBuilderExtensions.ReadBoundedBodyAsync(context.Request, 3, CancellationToken.None));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, (int)exception.StatusCode);
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}

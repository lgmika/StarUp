using StartupConnect.Shared.Responses;

namespace StartupConnect.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapStartupConnectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");

        api.MapGet("/", () => Results.Ok(ApiResponse<object>.Ok(new
        {
            name = "StartupConnect API",
            version = "v1"
        })))
        .WithName("ApiInfo");

        return endpoints;
    }
}

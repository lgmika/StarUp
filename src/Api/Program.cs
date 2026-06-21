using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StartupConnect.Api.Authorization;
using StartupConnect.Api.Extensions;
using StartupConnect.Api.Hubs;
using StartupConnect.Api.Middlewares;
using StartupConnect.Api.Observability;
using StartupConnect.Api.Realtime;
using StartupConnect.Api.Security;
using StartupConnect.Application;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Constants;
using StartupConnect.Infrastructure;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ObservabilityOptions>(builder.Configuration.GetSection("Observability"));
var observabilityOptions = builder.Configuration.GetSection("Observability").Get<ObservabilityOptions>() ?? new ObservabilityOptions();
if (observabilityOptions.UseJsonConsole)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<StartupConnectSecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<ApiRateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));
SecurityConfigurationValidator.Validate(builder.Configuration, builder.Environment);
var configuredSecurityOptions = builder.Configuration.GetSection("Security").Get<StartupConnectSecurityOptions>() ?? new StartupConnectSecurityOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = configuredSecurityOptions.MaxRequestBodySizeBytes;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(configuredSecurityOptions.HstsMaxAgeDays);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
[
    "http://localhost:3000",
    "http://localhost:5173",
    "http://127.0.0.1:3000",
    "http://127.0.0.1:5173"
];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
var rateLimitOptions = builder.Configuration.GetSection("RateLimiting").Get<ApiRateLimitOptions>() ?? new ApiRateLimitOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
            ? delay.TotalSeconds.ToString("0")
            : null;
        if (retryAfter is not null)
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            ErrorResponse.Fail("Too many requests", [new ErrorDetail("RateLimitExceeded", "Please retry later", null)]),
            cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (!rateLimitOptions.Enabled)
        {
            return RateLimitPartition.GetNoLimiter("disabled");
        }

        var path = httpContext.Request.Path;
        var permitLimit = rateLimitOptions.PermitLimit;
        var windowSeconds = rateLimitOptions.WindowSeconds;

        if (path.StartsWithSegments("/api/v1/auth"))
        {
            permitLimit = rateLimitOptions.AuthPermitLimit;
            windowSeconds = rateLimitOptions.AuthWindowSeconds;
        }
        else if (path.StartsWithSegments("/api/v1/webhooks"))
        {
            permitLimit = rateLimitOptions.WebhookPermitLimit;
            windowSeconds = rateLimitOptions.WebhookWindowSeconds;
        }

        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? httpContext.User.FindFirst("sub")?.Value}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = rateLimitOptions.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var knownProxies = builder.Configuration.GetSection("Security:KnownProxies").Get<string[]>() ?? [];
    foreach (var proxy in knownProxies)
    {
        if (System.Net.IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }

    var knownNetworks = builder.Configuration.GetSection("Security:KnownNetworks").Get<string[]>() ?? [];
    foreach (var network in knownNetworks)
    {
        var separator = network.LastIndexOf('/');
        if (separator > 0 &&
            System.Net.IPAddress.TryParse(network[..separator], out var prefix) &&
            int.TryParse(network[(separator + 1)..], out var prefixLength))
        {
            options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
        }
    }
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StartupConnect API",
        Version = "v1"
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    (path.StartsWithSegments("/hubs/startupconnect") || path.StartsWithSegments("/hubs/realtime")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.VerifiedUserOnly, policy =>
        policy.RequireRole(SystemRoles.VerifiedUser, SystemRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.BusinessOnly, policy =>
        policy.RequireRole(SystemRoles.Business, SystemRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.InvestorOnly, policy =>
        policy.RequireRole(SystemRoles.Investor, SystemRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.ModeratorOnly, policy =>
        policy.RequireRole(SystemRoles.Moderator, SystemRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(SystemRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.ModeratorOrAdmin, policy =>
        policy.RequireRole(SystemRoles.Moderator, SystemRoles.Admin));
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgresql");

var app = builder.Build();
var securityOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StartupConnectSecurityOptions>>().Value;

if (securityOptions.UseForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedDevelopmentDataAsync(app.Services);
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "StartupConnect API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment() && securityOptions.RequireHttpsRedirection)
{
    if (securityOptions.EnableHsts)
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/api/v1/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckExtensions.WriteStartupConnectHealthResponse
})
.DisableRateLimiting()
.WithName("HealthCheck");

app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckExtensions.WriteStartupConnectHealthResponse
})
.DisableRateLimiting()
.WithName("LivenessHealthCheck");

app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions
{
    ResponseWriter = HealthCheckExtensions.WriteStartupConnectHealthResponse
})
.DisableRateLimiting()
.WithName("ReadinessHealthCheck");

app.MapStartupConnectEndpoints();
app.MapHub<StartupConnectHub>("/hubs/startupconnect");
app.MapHub<StartupConnectHub>("/hubs/realtime");

app.Run();

public partial class Program;

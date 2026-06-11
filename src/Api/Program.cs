using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StartupConnect.Api.Authorization;
using StartupConnect.Api.Extensions;
using StartupConnect.Api.Middlewares;
using StartupConnect.Application;
using StartupConnect.Domain.Constants;
using StartupConnect.Infrastructure;
using StartupConnect.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
builder.Services.AddOpenApi();
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

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "StartupConnect API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/v1/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckExtensions.WriteStartupConnectHealthResponse
})
.WithName("HealthCheck");

app.MapStartupConnectEndpoints();

app.Run();

public partial class Program;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StartupConnect.Api.Extensions;
using StartupConnect.Api.Middlewares;
using StartupConnect.Application;
using StartupConnect.Infrastructure;
using StartupConnect.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

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

app.MapHealthChecks("/api/v1/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckExtensions.WriteStartupConnectHealthResponse
})
.WithName("HealthCheck");

app.MapStartupConnectEndpoints();

app.Run();

public partial class Program;

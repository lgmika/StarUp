using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StartupConnect.Application.AI.Interfaces;
using StartupConnect.Application.Applications.Interfaces;
using StartupConnect.Application.Auth.Interfaces;
using StartupConnect.Application.Investors.Interfaces;
using StartupConnect.Application.Moderation.Interfaces;
using StartupConnect.Application.Nda.Interfaces;
using StartupConnect.Application.Profiles.Interfaces;
using StartupConnect.Application.Projects.Interfaces;
using StartupConnect.Infrastructure.AI;
using StartupConnect.Infrastructure.Applications;
using StartupConnect.Infrastructure.Auth;
using StartupConnect.Infrastructure.Investors;
using StartupConnect.Infrastructure.Moderation;
using StartupConnect.Infrastructure.Nda;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Profiles;
using StartupConnect.Infrastructure.Projects;

namespace StartupConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = configuration["Jwt:Audience"] ?? string.Empty;
            options.SigningKey = configuration["Jwt:SigningKey"] ?? string.Empty;
            options.AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var accessTokenMinutes)
                ? accessTokenMinutes
                : 30;
            options.RefreshTokenDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var refreshTokenDays)
                ? refreshTokenDays
                : 14;
        });
        services.AddScoped<PasswordHasher>();
        services.AddScoped<SecureTokenGenerator>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IAIService, MockAIService>();
        services.AddScoped<IModeratorService, ModeratorService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IInvestorService, InvestorService>();
        services.AddScoped<INdaService, NdaService>();

        return services;
    }
}

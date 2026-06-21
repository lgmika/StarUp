using StartupConnect.Application.Auth.Dtos;

namespace StartupConnect.Api.Security;

public static class AuthCookieHelper
{
    public static AuthResponse PrepareClientResponse(HttpResponse response, AuthResponse authResponse, StartupConnectSecurityOptions options)
    {
        AppendRefreshTokenCookie(response, authResponse, options);
        return options.RefreshTokenCookie.Enabled
            ? authResponse with { RefreshToken = string.Empty }
            : authResponse;
    }

    public static void AppendRefreshTokenCookie(HttpResponse response, AuthResponse authResponse, StartupConnectSecurityOptions options)
    {
        if (!options.RefreshTokenCookie.Enabled)
        {
            return;
        }

        response.Cookies.Append(
            options.RefreshTokenCookie.Name,
            authResponse.RefreshToken,
            BuildCookieOptions(options, authResponse.RefreshTokenExpiresAt));
    }

    public static string? ReadRefreshTokenCookie(HttpRequest request, StartupConnectSecurityOptions options)
    {
        if (!options.RefreshTokenCookie.Enabled)
        {
            return null;
        }

        return request.Cookies.TryGetValue(options.RefreshTokenCookie.Name, out var refreshToken)
            ? refreshToken
            : null;
    }

    public static void DeleteRefreshTokenCookie(HttpResponse response, StartupConnectSecurityOptions options)
    {
        if (!options.RefreshTokenCookie.Enabled)
        {
            return;
        }

        response.Cookies.Delete(options.RefreshTokenCookie.Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = options.RefreshTokenCookie.Secure,
            SameSite = ParseSameSite(options.RefreshTokenCookie.SameSite),
            Path = "/"
        });
    }

    public static SameSiteMode ParseSameSite(string? sameSite)
    {
        return sameSite?.Trim().ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            _ => SameSiteMode.Strict
        };
    }

    private static CookieOptions BuildCookieOptions(StartupConnectSecurityOptions options, DateTimeOffset expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = options.RefreshTokenCookie.Secure,
            SameSite = ParseSameSite(options.RefreshTokenCookie.SameSite),
            Expires = expiresAt,
            MaxAge = TimeSpan.FromDays(options.RefreshTokenCookie.Days),
            Path = "/"
        };
    }
}

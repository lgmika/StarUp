namespace StartupConnect.Api.Security;

public sealed class StartupConnectSecurityOptions
{
    public bool UseForwardedHeaders { get; set; } = true;

    public bool RequireHttpsRedirection { get; set; } = true;

    public bool EnableHsts { get; set; } = true;

    public int HstsMaxAgeDays { get; set; } = 365;

    public bool EnableSecurityHeaders { get; set; } = true;

    public long MaxRequestBodySizeBytes { get; set; } = 25 * 1024 * 1024;

    public string[] KnownProxies { get; set; } = [];

    public string[] KnownNetworks { get; set; } = [];

    public RefreshTokenCookieSettings RefreshTokenCookie { get; set; } = new();
}

public sealed class RefreshTokenCookieSettings
{
    public bool Enabled { get; set; }

    public string Name { get; set; } = "__Host-startupconnect-refresh";

    public bool Secure { get; set; } = true;

    public string SameSite { get; set; } = "Strict";

    public int Days { get; set; } = 14;
}

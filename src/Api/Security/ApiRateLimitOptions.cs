namespace StartupConnect.Api.Security;

public sealed class ApiRateLimitOptions
{
    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 120;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; } = 0;

    public int AuthPermitLimit { get; set; } = 20;

    public int AuthWindowSeconds { get; set; } = 60;

    public int WebhookPermitLimit { get; set; } = 300;

    public int WebhookWindowSeconds { get; set; } = 60;
}

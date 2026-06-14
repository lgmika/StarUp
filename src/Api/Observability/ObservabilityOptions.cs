namespace StartupConnect.Api.Observability;

public sealed class ObservabilityOptions
{
    public bool EnableRequestLogging { get; set; } = true;

    public bool UseJsonConsole { get; set; }

    public string CorrelationHeaderName { get; set; } = "X-Correlation-Id";

    public int SlowRequestThresholdMs { get; set; } = 1000;
}

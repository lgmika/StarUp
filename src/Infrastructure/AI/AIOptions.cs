namespace StartupConnect.Infrastructure.AI;

public sealed class AIOptions
{
    public string Provider { get; set; } = "Mock";

    public int DailyQuota { get; set; } = 20;

    public OllamaOptions Ollama { get; set; } = new();
}

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "llama3.1";

    public int TimeoutSeconds { get; set; } = 120;
}

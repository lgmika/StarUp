namespace StartupConnect.Infrastructure.Email;

public sealed class EmailOutboxOptions
{
    public bool Enabled { get; set; } = true;

    public int PollSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 10;

    public int LeaseSeconds { get; set; } = 600;

    public string EncryptionKey { get; set; } = string.Empty;
}

namespace StartupConnect.Domain.Entities;

public sealed class EmailOutboxMessage : BaseEntity
{
    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public string Recipient { get; set; } = string.Empty;

    public string Template { get; set; } = string.Empty;

    public string ProtectedPayload { get; set; } = string.Empty;

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? LeaseId { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public string? LastError { get; set; }
}

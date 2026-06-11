namespace StartupConnect.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? Reason { get; set; }

    public string? MetadataJson { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}


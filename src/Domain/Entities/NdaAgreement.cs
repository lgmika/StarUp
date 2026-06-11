namespace StartupConnect.Domain.Entities;

public sealed class NdaAgreement : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid TemplateId { get; set; }

    public NdaTemplate Template { get; set; } = null!;

    public Guid TemplateVersionId { get; set; }

    public NdaTemplateVersion TemplateVersion { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string AgreementSnapshot { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
}


namespace StartupConnect.Domain.Entities;

public sealed class NdaTemplateVersion : BaseEntity
{
    public Guid TemplateId { get; set; }

    public NdaTemplate Template { get; set; } = null!;

    public int VersionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
}


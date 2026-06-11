namespace StartupConnect.Domain.Entities;

public sealed class NdaTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<NdaTemplateVersion> Versions { get; set; } = [];
}


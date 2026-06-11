namespace StartupConnect.Domain.Entities;

public sealed class ApplicationAttachment : BaseEntity
{
    public Guid ApplicationId { get; set; }

    public ProjectApplication Application { get; set; } = null!;

    public Guid FileId { get; set; }

    public StoredFile File { get; set; } = null!;
}


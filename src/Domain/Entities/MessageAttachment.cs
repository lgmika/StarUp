namespace StartupConnect.Domain.Entities;

public sealed class MessageAttachment : BaseEntity
{
    public Guid MessageId { get; set; }

    public Message Message { get; set; } = null!;

    public Guid FileId { get; set; }

    public StoredFile File { get; set; } = null!;
}

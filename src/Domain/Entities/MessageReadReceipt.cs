namespace StartupConnect.Domain.Entities;

public sealed class MessageReadReceipt : BaseEntity
{
    public Guid MessageId { get; set; }

    public Message Message { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
}

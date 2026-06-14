namespace StartupConnect.Domain.Entities;

public sealed class Message : BaseEntity
{
    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public Guid SenderUserId { get; set; }

    public User SenderUser { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}

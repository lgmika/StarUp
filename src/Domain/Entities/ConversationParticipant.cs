namespace StartupConnect.Domain.Entities;

public sealed class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTimeOffset? LastReadAt { get; set; }

    public bool IsMuted { get; set; }
}

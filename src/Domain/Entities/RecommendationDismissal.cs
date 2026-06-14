using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class RecommendationDismissal : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RecommendationId { get; set; }

    public RecommendationType Type { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? RecommendedUserId { get; set; }
}

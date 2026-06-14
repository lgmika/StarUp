using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    public ConversationType Type { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? ApplicationId { get; set; }

    public ProjectApplication? Application { get; set; }

    public Guid? InvestorInterestId { get; set; }

    public InvestorProjectInterest? InvestorInterest { get; set; }

    public string? Title { get; set; }

    public bool IsArchived { get; set; }
}

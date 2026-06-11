using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class InvestorProjectInterest : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid InvestorUserId { get; set; }

    public User InvestorUser { get; set; } = null!;

    public string Message { get; set; } = string.Empty;

    public InvestorInterestStatus Status { get; set; } = InvestorInterestStatus.Pending;

    public string? FounderResponse { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }
}


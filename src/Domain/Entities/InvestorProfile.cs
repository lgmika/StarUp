namespace StartupConnect.Domain.Entities;

public sealed class InvestorProfile : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string DisplayName { get; set; } = string.Empty;

    public string? OrganizationName { get; set; }

    public string? Bio { get; set; }

    public string? InvestmentFocus { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? LinkedInUrl { get; set; }

    public decimal? MinTicketSize { get; set; }

    public decimal? MaxTicketSize { get; set; }
}


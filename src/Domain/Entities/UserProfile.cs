using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Headline { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string? PhoneNumber { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? GitHubUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public ContactVisibility ContactVisibility { get; set; } = ContactVisibility.Private;
}


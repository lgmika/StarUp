using StartupConnect.Domain.Enums;

namespace StartupConnect.Domain.Entities;

public sealed class AIRequest : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public AIRequestType RequestType { get; set; }

    public string PromptSnapshot { get; set; } = string.Empty;

    public string Provider { get; set; } = "Mock";

    public bool IsSuccessful { get; set; } = true;
}


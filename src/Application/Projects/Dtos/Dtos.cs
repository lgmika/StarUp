using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Projects.Dtos;

public sealed record ProjectSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    ProjectStatus Status,
    ProjectStage Stage,
    ProjectVisibility Visibility,
    bool IsRecruiting,
    DateTimeOffset CreatedAt);

public sealed record ProjectDetailDto(
    Guid Id,
    Guid OwnerUserId,
    string Title,
    string Slug,
    string Summary,
    string Problem,
    string Solution,
    string? TargetMarket,
    string? BusinessModel,
    string? FundingNeeds,
    string? PitchDeckUrl,
    ProjectStatus Status,
    ProjectStage Stage,
    bool IsRecruiting,
    ProjectVisibility Visibility,
    bool RequiresNda,
    IReadOnlyCollection<ProjectRequiredRoleDto> RequiredRoles,
    IReadOnlyCollection<ProjectSkillDto> RequiredSkills,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateProjectDraftRequest(
    string Title,
    string Summary,
    string Problem,
    string Solution,
    ProjectStage Stage,
    ProjectVisibility Visibility);

public sealed record UpdateProjectRequest(
    string Title,
    string Summary,
    string Problem,
    string Solution,
    string? TargetMarket,
    string? BusinessModel,
    string? FundingNeeds,
    string? PitchDeckUrl,
    ProjectStage Stage,
    ProjectVisibility Visibility,
    bool IsRecruiting,
    IReadOnlyCollection<UpsertProjectRequiredRoleDto> RequiredRoles,
    IReadOnlyCollection<Guid> RequiredSkillIds);

public sealed record UpsertProjectRequiredRoleDto(
    string RoleName,
    string? Description,
    int Slots,
    bool IsOpen);

public sealed record ProjectRequiredRoleDto(
    Guid Id,
    string RoleName,
    string? Description,
    int Slots,
    bool IsOpen);

public sealed record ProjectSkillDto(Guid Id, string Name);

public sealed record ProjectVersionDto(
    Guid Id,
    int VersionNumber,
    string ChangeReason,
    DateTimeOffset CreatedAt);


using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Activities.Dtos;

public sealed record ActivityDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid? ActorUserId,
    string? ActorName,
    ActivityType Type,
    ActivityVisibility Visibility,
    string Title,
    string? Message,
    string? TargetType,
    Guid? TargetId,
    DateTimeOffset CreatedAt);

public sealed record ActivityListResponse(
    IReadOnlyCollection<ActivityDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record ActivityQuery(int Page = 1, int PageSize = 20);

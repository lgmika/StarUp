using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Notifications.Dtos;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? ResourceType,
    Guid? ResourceId,
    string? ActionUrl,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

public sealed record NotificationListResponse(
    IReadOnlyCollection<NotificationDto> Items,
    int Total,
    int UnreadCount,
    int Page,
    int PageSize);

public sealed record NotificationQuery(
    string? Status = null,
    NotificationType? Type = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CreateNotificationRequest(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    string? ResourceType = null,
    Guid? ResourceId = null,
    string? ActionUrl = null);

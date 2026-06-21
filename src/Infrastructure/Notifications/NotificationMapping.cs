using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Domain.Entities;

namespace StartupConnect.Infrastructure.Notifications;

internal static class NotificationMapping
{
    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ResourceType,
            notification.ResourceId,
            notification.ActionUrl,
            notification.ReadAt.HasValue,
            notification.ReadAt,
            notification.CreatedAt);
    }
}

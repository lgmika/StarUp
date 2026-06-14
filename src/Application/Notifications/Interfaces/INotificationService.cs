using System.Security.Claims;
using StartupConnect.Application.Notifications.Dtos;

namespace StartupConnect.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponse> GetMyNotificationsAsync(
        ClaimsPrincipal principal,
        NotificationQuery query,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<NotificationDto> MarkAsReadAsync(
        ClaimsPrincipal principal,
        Guid notificationId,
        CancellationToken cancellationToken);

    Task<int> MarkAllAsReadAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        ClaimsPrincipal principal,
        Guid notificationId,
        CancellationToken cancellationToken);

    Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken);
}

using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Application.Notifications.Interfaces;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Notifications;

public sealed class NotificationService(
    AppDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    ISystemSettingReader systemSettingReader) : INotificationService
{
    private const int MaxPageSize = 100;

    public async Task<NotificationListResponse> GetMyNotificationsAsync(
        ClaimsPrincipal principal,
        NotificationQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var notifications = ApplyFilters(
            dbContext.Notifications.Where(item => item.UserId == userId && !item.IsDeleted),
            query);

        var total = await notifications.CountAsync(cancellationToken);
        var unreadCount = await dbContext.Notifications.CountAsync(
            item => item.UserId == userId && !item.IsDeleted && item.ReadAt == null,
            cancellationToken);

        var items = await notifications
            .OrderByDescending(item => item.CreatedAt)
            .Skip(Pagination.GetOffset(page, pageSize))
            .Take(pageSize)
            .Select(item => Map(item))
            .ToArrayAsync(cancellationToken);

        return new NotificationListResponse(items, total, unreadCount, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        return await dbContext.Notifications.CountAsync(
            item => item.UserId == userId && !item.IsDeleted && item.ReadAt == null,
            cancellationToken);
    }

    public async Task<NotificationDto> MarkAsReadAsync(
        ClaimsPrincipal principal,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var notification = await GetOwnedNotificationAsync(userId, notificationId, cancellationToken);

        notification.ReadAt ??= DateTimeOffset.UtcNow;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = Map(notification);
        await realtimeNotifier.NotificationReadAsync(userId, notification.Id, cancellationToken);
        return result;
    }

    public async Task<int> MarkAllAsReadAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var now = DateTimeOffset.UtcNow;
        var updatedCount = await dbContext.Notifications
            .Where(item => item.UserId == userId && !item.IsDeleted && item.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.ReadAt, now)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);

        if (updatedCount > 0)
        {
            await realtimeNotifier.NotificationsReadAllAsync(userId, cancellationToken);
        }

        return updatedCount;
    }

    public async Task DeleteAsync(
        ClaimsPrincipal principal,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var notification = await GetOwnedNotificationAsync(userId, notificationId, cancellationToken);

        notification.IsDeleted = true;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationDto> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCreate(request);

        if (!await systemSettingReader.GetBooleanAsync("Notifications.Enabled", true, cancellationToken))
        {
            return new NotificationDto(
                Guid.Empty,
                request.Type,
                request.Title.Trim(),
                request.Message.Trim(),
                request.ResourceType?.Trim(),
                request.ResourceId,
                request.ActionUrl?.Trim(),
                false,
                null,
                DateTimeOffset.UtcNow);
        }

        var duplicateExists = request.ResourceId.HasValue && await dbContext.Notifications.AnyAsync(
            item => item.UserId == request.UserId &&
                !item.IsDeleted &&
                item.ReadAt == null &&
                item.Type == request.Type &&
                item.ResourceId == request.ResourceId &&
                item.ResourceType == request.ResourceType &&
                item.Title == request.Title,
            cancellationToken);

        if (duplicateExists)
        {
            var existing = await dbContext.Notifications
                .Where(item => item.UserId == request.UserId &&
                    !item.IsDeleted &&
                    item.ReadAt == null &&
                    item.Type == request.Type &&
                    item.ResourceId == request.ResourceId &&
                    item.ResourceType == request.ResourceType &&
                    item.Title == request.Title)
                .OrderByDescending(item => item.CreatedAt)
                .FirstAsync(cancellationToken);

            return Map(existing);
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            ResourceType = string.IsNullOrWhiteSpace(request.ResourceType) ? null : request.ResourceType.Trim(),
            ResourceId = request.ResourceId,
            ActionUrl = string.IsNullOrWhiteSpace(request.ActionUrl) ? null : request.ActionUrl.Trim()
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = Map(notification);
        await realtimeNotifier.NotificationCreatedAsync(notification.UserId, result, cancellationToken);
        return result;
    }

    private static IQueryable<Notification> ApplyFilters(IQueryable<Notification> query, NotificationQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (filter.Status.Equals("read", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item => item.ReadAt != null);
            }
            else if (filter.Status.Equals("unread", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item => item.ReadAt == null);
            }
            else if (!filter.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException([new ErrorDetail("InvalidStatus", "Status must be all, read, or unread", "status")]);
            }
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(item => item.Type == filter.Type.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(item => item.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(item => item.CreatedAt <= filter.To.Value);
        }

        return query;
    }

    private async Task<Notification> GetOwnedNotificationAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        return await dbContext.Notifications.FirstOrDefaultAsync(
            item => item.Id == notificationId && item.UserId == userId && !item.IsDeleted,
            cancellationToken)
            ?? throw new ApiException("Notification not found", HttpStatusCode.NotFound);
    }

    private static void ValidateCreate(CreateNotificationRequest request)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ValidationException([new ErrorDetail("Required", "User id is required", "userId")]);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException([new ErrorDetail("Required", "Title is required", "title")]);
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ValidationException([new ErrorDetail("Required", "Message is required", "message")]);
        }
    }

    private static NotificationDto Map(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ResourceType,
            notification.ResourceId,
            notification.ActionUrl,
            notification.ReadAt is not null,
            notification.ReadAt,
            notification.CreatedAt);
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("nameid")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}

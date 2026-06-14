using StartupConnect.Application.Realtime.Interfaces;

namespace StartupConnect.Infrastructure.Realtime;

public sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task NotifyProjectAsync(Guid projectId, string eventName, object payload, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task NotifyConversationAsync(Guid conversationId, string eventName, object payload, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task NotificationCreatedAsync(Guid userId, object notification, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task NotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task NotificationsReadAllAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task MessageCreatedAsync(Guid conversationId, object message, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task MessageDeletedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task MessageReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ProjectStatusChangedAsync(Guid projectId, object project, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ApplicationStatusChangedAsync(Guid userId, Guid projectId, object application, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task InvestorInterestChangedAsync(Guid projectId, Guid investorUserId, object interest, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task InterviewChangedAsync(Guid projectId, IReadOnlyCollection<Guid> participantUserIds, object interview, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ReportChangedAsync(Guid reportId, object report, CancellationToken cancellationToken) => Task.CompletedTask;
}

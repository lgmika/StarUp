namespace StartupConnect.Application.Realtime.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken);

    Task NotifyProjectAsync(Guid projectId, string eventName, object payload, CancellationToken cancellationToken);

    Task NotifyConversationAsync(Guid conversationId, string eventName, object payload, CancellationToken cancellationToken);

    Task NotificationCreatedAsync(Guid userId, object notification, CancellationToken cancellationToken);

    Task NotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);

    Task NotificationsReadAllAsync(Guid userId, CancellationToken cancellationToken);

    Task MessageCreatedAsync(Guid conversationId, object message, CancellationToken cancellationToken);

    Task MessageDeletedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken);

    Task MessageReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);

    Task ProjectStatusChangedAsync(Guid projectId, object project, CancellationToken cancellationToken);

    Task ApplicationStatusChangedAsync(Guid userId, Guid projectId, object application, CancellationToken cancellationToken);

    Task InvestorInterestChangedAsync(Guid projectId, Guid investorUserId, object interest, CancellationToken cancellationToken);

    Task InterviewChangedAsync(Guid projectId, IReadOnlyCollection<Guid> participantUserIds, object interview, CancellationToken cancellationToken);

    Task ReportChangedAsync(Guid reportId, object report, CancellationToken cancellationToken);
}

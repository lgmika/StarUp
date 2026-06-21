using Microsoft.AspNetCore.SignalR;
using StartupConnect.Api.Hubs;
using StartupConnect.Application.Realtime;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Application.Admin.Interfaces;

namespace StartupConnect.Api.Realtime;

public sealed class SignalRRealtimeNotifier(
    IHubContext<StartupConnectHub> hubContext,
    ISystemSettingReader systemSettingReader) : IRealtimeNotifier
{
    public async Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return;
        }

        await hubContext.Clients
            .Group(RealtimeGroups.User(userId))
            .SendAsync(eventName, payload, cancellationToken);
    }

    public async Task NotifyProjectAsync(Guid projectId, string eventName, object payload, CancellationToken cancellationToken)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return;
        }

        await hubContext.Clients
            .Group(RealtimeGroups.Project(projectId))
            .SendAsync(eventName, payload, cancellationToken);
    }

    public async Task NotifyConversationAsync(Guid conversationId, string eventName, object payload, CancellationToken cancellationToken)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return;
        }

        await hubContext.Clients
            .Group(RealtimeGroups.Conversation(conversationId))
            .SendAsync(eventName, payload, cancellationToken);
    }

    public Task NotificationCreatedAsync(Guid userId, object notification, CancellationToken cancellationToken)
    {
        return NotifyUserAsync(userId, RealtimeEventNames.NotificationCreated, notification, cancellationToken);
    }

    public Task NotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        return NotifyUserAsync(
            userId,
            RealtimeEventNames.NotificationRead,
            new { notificationId },
            cancellationToken);
    }

    public Task NotificationsReadAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        return NotifyUserAsync(
            userId,
            RealtimeEventNames.NotificationsReadAll,
            new { userId },
            cancellationToken);
    }

    public Task MessageCreatedAsync(Guid conversationId, object message, CancellationToken cancellationToken)
    {
        return NotifyConversationAsync(conversationId, RealtimeEventNames.MessageCreated, message, cancellationToken);
    }

    public Task MessageDeletedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        return NotifyConversationAsync(
            conversationId,
            RealtimeEventNames.MessageDeleted,
            new { conversationId, messageId },
            cancellationToken);
    }

    public Task MessageReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        return NotifyConversationAsync(
            conversationId,
            RealtimeEventNames.MessageRead,
            new { conversationId, userId },
            cancellationToken);
    }

    public Task ProjectStatusChangedAsync(Guid projectId, object project, CancellationToken cancellationToken)
    {
        return NotifyProjectAsync(projectId, RealtimeEventNames.ProjectStatusChanged, project, cancellationToken);
    }

    public async Task ApplicationStatusChangedAsync(Guid userId, Guid projectId, object application, CancellationToken cancellationToken)
    {
        await NotifyUserAsync(userId, RealtimeEventNames.ApplicationStatusChanged, application, cancellationToken);
        await NotifyProjectAsync(projectId, RealtimeEventNames.ApplicationStatusChanged, application, cancellationToken);
        await NotifyUserAsync(userId, RealtimeEventNames.ApplicationStatusChangedV2, application, cancellationToken);
        await NotifyProjectAsync(projectId, RealtimeEventNames.ApplicationStatusChangedV2, application, cancellationToken);
    }

    public async Task InvestorInterestChangedAsync(Guid projectId, Guid investorUserId, object interest, CancellationToken cancellationToken)
    {
        await NotifyUserAsync(investorUserId, RealtimeEventNames.InvestorInterestChanged, interest, cancellationToken);
        await NotifyProjectAsync(projectId, RealtimeEventNames.InvestorInterestChanged, interest, cancellationToken);
    }

    public async Task InterviewChangedAsync(Guid projectId, IReadOnlyCollection<Guid> participantUserIds, object interview, CancellationToken cancellationToken)
    {
        await NotifyProjectAsync(projectId, RealtimeEventNames.InterviewChanged, interview, cancellationToken);

        foreach (var userId in participantUserIds.Distinct())
        {
            await NotifyUserAsync(userId, RealtimeEventNames.InterviewChanged, interview, cancellationToken);
        }
    }

    public async Task ReportChangedAsync(Guid reportId, object report, CancellationToken cancellationToken)
    {
        if (!await IsEnabledAsync(cancellationToken))
        {
            return;
        }

        await hubContext.Clients
            .Group("moderators")
            .SendAsync(RealtimeEventNames.ReportChanged, new { reportId, report }, cancellationToken);
    }

    private Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        return systemSettingReader.GetBooleanAsync("Realtime.Enabled", true, cancellationToken);
    }
}

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Api.Realtime;
using StartupConnect.Domain.Constants;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;

namespace StartupConnect.Api.Hubs;

[Authorize]
public sealed class StartupConnectHub(AppDbContext dbContext) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.User(userId));
        if (Context.User?.IsInRole(SystemRoles.Moderator) == true || Context.User?.IsInRole(SystemRoles.Admin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "moderators");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinProject(Guid projectId)
    {
        var userId = GetUserId();
        if (!await HasProjectAccessAsync(projectId, userId))
        {
            throw new HubException("Project access denied");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Project(projectId));
    }

    public Task LeaveProject(Guid projectId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Project(projectId));
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        var isParticipant = await dbContext.ConversationParticipants.AnyAsync(
            participant => participant.ConversationId == conversationId && participant.UserId == userId,
            Context.ConnectionAborted);

        if (!isParticipant)
        {
            throw new HubException("Conversation access denied");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Conversation(conversationId));
    }

    public Task LeaveConversation(Guid conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Conversation(conversationId));
    }

    private async Task<bool> HasProjectAccessAsync(Guid projectId, Guid userId)
    {
        return await dbContext.ProjectMembers.AnyAsync(
            member => member.ProjectId == projectId && member.UserId == userId && member.IsActive,
            Context.ConnectionAborted) ||
            await dbContext.ProjectAccessGrants.AnyAsync(
                grant => grant.ProjectId == projectId &&
                    grant.UserId == userId &&
                    (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow),
                Context.ConnectionAborted) ||
            await dbContext.InvestorProjectInterests.AnyAsync(
                interest => interest.ProjectId == projectId &&
                    interest.InvestorUserId == userId &&
                    interest.Status == InvestorInterestStatus.Accepted,
                Context.ConnectionAborted);
    }

    private Guid GetUserId()
    {
        var userIdValue =
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            Context.User?.FindFirst("sub")?.Value ??
            Context.User?.FindFirst("nameid")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}

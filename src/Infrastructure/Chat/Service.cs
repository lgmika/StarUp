using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Chat.Dtos;
using StartupConnect.Application.Chat.Interfaces;
using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Application.Notifications.Interfaces;
using StartupConnect.Application.Realtime;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Chat;

public sealed class ChatService(
    AppDbContext dbContext,
    INotificationService notificationService,
    IRealtimeNotifier realtimeNotifier) : IChatService
{
    private const int MaxMessageLength = 4000;
    private const int MaxPageSize = 100;
    private const int DailyMessageLimit = 200;
    private static readonly string[] BlockedContentTypes =
    [
        "application/x-msdownload",
        "application/x-msdos-program",
        "application/x-sh",
        "application/x-bat"
    ];

    public async Task<ConversationDto> CreateConversationAsync(ClaimsPrincipal principal, CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        if (request.ParticipantUserIds is null)
        {
            throw new ValidationException([new ErrorDetail("ParticipantsRequired", "Conversation participants are required", "participantUserIds")]);
        }

        if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Trim().Length > 200)
        {
            throw new ValidationException([new ErrorDetail("TitleTooLong", "Conversation title must be at most 200 characters", "title")]);
        }

        var participantIds = request.ParticipantUserIds.Where(id => id != Guid.Empty).Append(actorUserId).Distinct().ToArray();
        if (participantIds.Length < 2)
        {
            throw new ValidationException([new ErrorDetail("ParticipantsRequired", "Conversation requires at least two participants", "participantUserIds")]);
        }

        if (participantIds.Length > 100)
        {
            throw new ValidationException([new ErrorDetail("TooManyParticipants", "Conversation supports at most 100 participants", "participantUserIds")]);
        }

        await EnsureCanCreateConversationAsync(actorUserId, request, participantIds, cancellationToken);
        await using var directTransaction = request.Type == ConversationType.Direct
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (directTransaction is not null)
        {
            var lockResource = string.Join(':', participantIds.Order());
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockResource}, 0))",
                cancellationToken);
        }

        var duplicateDirect = request.Type == ConversationType.Direct
            ? await FindDuplicateDirectConversationAsync(participantIds, cancellationToken)
            : null;
        if (duplicateDirect is not null)
        {
            var existingResult = await MapConversationAsync(duplicateDirect.Id, cancellationToken);
            await directTransaction!.CommitAsync(cancellationToken);
            return existingResult;
        }

        var conversation = new Conversation
        {
            Type = request.Type,
            ProjectId = request.ProjectId,
            ApplicationId = request.ApplicationId,
            InvestorInterestId = request.InvestorInterestId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim()
        };

        dbContext.Conversations.Add(conversation);
        foreach (var participantId in participantIds)
        {
            dbContext.ConversationParticipants.Add(new ConversationParticipant
            {
                Conversation = conversation,
                UserId = participantId
            });
        }

        AddAudit(actorUserId, "Conversation.Create", "Conversation", conversation.Id, conversation.Type.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        if (directTransaction is not null)
        {
            await directTransaction.CommitAsync(cancellationToken);
        }

        var result = await MapConversationAsync(conversation.Id, cancellationToken);
        foreach (var participantId in participantIds)
        {
            await realtimeNotifier.NotifyUserAsync(participantId, RealtimeEventNames.ConversationCreated, result, cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyCollection<ConversationDto>> GetMyConversationsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var ids = await dbContext.Conversations
            .Where(conversation => dbContext.ConversationParticipants.Any(participant =>
                participant.ConversationId == conversation.Id && participant.UserId == userId))
            .OrderByDescending(conversation => dbContext.Messages
                .Where(message => message.ConversationId == conversation.Id)
                .Max(message => (DateTimeOffset?)message.CreatedAt) ?? conversation.CreatedAt)
            .Take(100)
            .Select(conversation => conversation.Id)
            .ToArrayAsync(cancellationToken);

        var result = new List<ConversationDto>();
        foreach (var id in ids)
        {
            result.Add(await MapConversationAsync(id, cancellationToken));
        }

        return result.OrderByDescending(item => item.LastMessageAt ?? item.CreatedAt).ToArray();
    }

    public async Task<ConversationDto> GetConversationAsync(ClaimsPrincipal principal, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureParticipantAsync(conversationId, userId, cancellationToken);
        return await MapConversationAsync(conversationId, cancellationToken);
    }

    public async Task<MessageListResponse> GetMessagesAsync(ClaimsPrincipal principal, Guid conversationId, string? beforeCursor, int pageSize, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureParticipantAsync(conversationId, userId, cancellationToken);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Messages
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .AsQueryable();

        if (DateTimeOffset.TryParse(beforeCursor, out var before))
        {
            query = query.Where(message => message.CreatedAt < before);
        }

        var messages = await query.Take(pageSize + 1).ToArrayAsync(cancellationToken);
        var items = new List<MessageDto>();
        foreach (var message in messages.Take(pageSize))
        {
            items.Add(await MapMessageAsync(message.Id, cancellationToken));
        }

        var nextCursor = messages.Length > pageSize ? messages[pageSize - 1].CreatedAt.ToString("O") : null;
        return new MessageListResponse(items, nextCursor);
    }

    public async Task<MessageDto> SendMessageAsync(ClaimsPrincipal principal, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureParticipantAsync(conversationId, userId, cancellationToken);
        ValidateMessage(request.Content);
        await EnsureMessageRateLimitAsync(userId, cancellationToken);
        var attachmentFileIds = request.AttachmentFileIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
        await EnsureAttachmentsAllowedAsync(userId, attachmentFileIds, cancellationToken);

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            Content = request.Content.Trim()
        };
        dbContext.Messages.Add(message);

        foreach (var fileId in attachmentFileIds)
        {
            dbContext.MessageAttachments.Add(new MessageAttachment { Message = message, FileId = fileId });
        }

        AddAudit(userId, "Message.Send", "Conversation", conversationId, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyParticipantsAsync(conversationId, userId, message.Id, cancellationToken);
        var result = await MapMessageAsync(message.Id, cancellationToken);
        await realtimeNotifier.MessageCreatedAsync(conversationId, result, cancellationToken);
        return result;
    }

    public async Task MarkReadAsync(ClaimsPrincipal principal, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var participant = await dbContext.ConversationParticipants.FirstOrDefaultAsync(item => item.ConversationId == conversationId && item.UserId == userId, cancellationToken)
            ?? throw new ApiException("Conversation not found", HttpStatusCode.NotFound);

        var now = DateTimeOffset.UtcNow;
        participant.LastReadAt = now;
        participant.UpdatedAt = now;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO message_read_receipts ("Id", "MessageId", "UserId", "ReadAt", "CreatedAt", "UpdatedAt")
            SELECT gen_random_uuid(), message."Id", {userId}, {now}, {now}, NULL
            FROM messages AS message
            WHERE message."ConversationId" = {conversationId}
              AND message."SenderUserId" <> {userId}
              AND message."CreatedAt" <= {now}
            ON CONFLICT ("MessageId", "UserId") DO NOTHING
            """, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await realtimeNotifier.MessageReadAsync(conversationId, userId, cancellationToken);
    }

    public async Task DeleteMessageAsync(ClaimsPrincipal principal, Guid messageId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var message = await dbContext.Messages.FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken)
            ?? throw new ApiException("Message not found", HttpStatusCode.NotFound);
        if (message.SenderUserId != userId)
        {
            throw new ApiException("You can only delete your own messages", HttpStatusCode.Forbidden);
        }

        message.IsDeleted = true;
        message.DeletedAt = DateTimeOffset.UtcNow;
        message.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(userId, "Message.Delete", "Message", message.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.MessageDeletedAsync(message.ConversationId, message.Id, cancellationToken);
    }

    private async Task EnsureCanCreateConversationAsync(Guid actorUserId, CreateConversationRequest request, IReadOnlyCollection<Guid> participantIds, CancellationToken cancellationToken)
    {
        var usersExist = await dbContext.Users.CountAsync(user => participantIds.Contains(user.Id) && !user.IsDeleted, cancellationToken) == participantIds.Count;
        if (!usersExist)
        {
            throw new ApiException("One or more conversation participants were not found", HttpStatusCode.NotFound);
        }

        switch (request.Type)
        {
            case ConversationType.Project:
                if (!request.ProjectId.HasValue || !await HasProjectAccessAsync(request.ProjectId.Value, actorUserId, cancellationToken))
                {
                    throw new ApiException("Project access is required to create project conversation", HttpStatusCode.Forbidden);
                }
                break;
            case ConversationType.Application:
                if (!request.ApplicationId.HasValue || !await CanAccessApplicationAsync(request.ApplicationId.Value, actorUserId, cancellationToken))
                {
                    throw new ApiException("Application access is required to create application conversation", HttpStatusCode.Forbidden);
                }
                break;
            case ConversationType.Investor:
                if (!request.InvestorInterestId.HasValue || !await CanAccessInvestorInterestAsync(request.InvestorInterestId.Value, actorUserId, cancellationToken))
                {
                    throw new ApiException("Investor interest access is required to create investor conversation", HttpStatusCode.Forbidden);
                }
                break;
        }
    }

    private async Task<Conversation?> FindDuplicateDirectConversationAsync(IReadOnlyCollection<Guid> participantIds, CancellationToken cancellationToken)
    {
        if (participantIds.Count != 2) return null;
        return await dbContext.Conversations.FirstOrDefaultAsync(
            conversation =>
                conversation.Type == ConversationType.Direct &&
                dbContext.ConversationParticipants.Count(participant => participant.ConversationId == conversation.Id) == 2 &&
                dbContext.ConversationParticipants.All(participant =>
                    participant.ConversationId != conversation.Id || participantIds.Contains(participant.UserId)),
            cancellationToken);
    }

    private async Task EnsureParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ConversationParticipants.AnyAsync(item => item.ConversationId == conversationId && item.UserId == userId, cancellationToken);
        if (!exists)
        {
            throw new ApiException("Conversation not found", HttpStatusCode.NotFound);
        }
    }

    private async Task<bool> HasProjectAccessAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId && member.IsActive, cancellationToken) ||
            await dbContext.ProjectAccessGrants.AnyAsync(grant => grant.ProjectId == projectId && grant.UserId == userId && (grant.ExpiresAt == null || grant.ExpiresAt > DateTimeOffset.UtcNow), cancellationToken);
    }

    private async Task<bool> CanAccessApplicationAsync(Guid applicationId, Guid userId, CancellationToken cancellationToken)
    {
        var application = await dbContext.ProjectApplications.FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken);
        if (application is null) return false;
        return application.ApplicantUserId == userId ||
            await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == application.ProjectId && member.UserId == userId && member.IsActive && (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder), cancellationToken);
    }

    private async Task<bool> CanAccessInvestorInterestAsync(Guid interestId, Guid userId, CancellationToken cancellationToken)
    {
        var interest = await dbContext.InvestorProjectInterests.FirstOrDefaultAsync(item => item.Id == interestId, cancellationToken);
        if (interest is null) return false;
        return interest.InvestorUserId == userId ||
            await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == interest.ProjectId && member.UserId == userId && member.IsActive && (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder), cancellationToken);
    }

    private async Task EnsureMessageRateLimitAsync(Guid userId, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var count = await dbContext.Messages.CountAsync(message => message.SenderUserId == userId && message.CreatedAt >= since, cancellationToken);
        if (count >= DailyMessageLimit)
        {
            throw new ApiException("Daily message limit exceeded", HttpStatusCode.TooManyRequests);
        }
    }

    private async Task EnsureAttachmentsAllowedAsync(Guid userId, IReadOnlyCollection<Guid> fileIds, CancellationToken cancellationToken)
    {
        if (fileIds.Count == 0) return;
        var files = await dbContext.Files.Where(file => fileIds.Contains(file.Id)).ToArrayAsync(cancellationToken);
        if (files.Length != fileIds.Count)
        {
            throw new ApiException("One or more attachments were not found", HttpStatusCode.NotFound);
        }

        foreach (var file in files)
        {
            if (file.OwnerUserId != userId || file.IsDeleted)
            {
                throw new ApiException("You can only attach your own active files", HttpStatusCode.Forbidden);
            }

            if (BlockedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                throw new ApiException("Attachment file type is not allowed", HttpStatusCode.BadRequest);
            }
        }
    }

    private async Task NotifyParticipantsAsync(Guid conversationId, Guid senderUserId, Guid messageId, CancellationToken cancellationToken)
    {
        var senderEmail = await dbContext.Users.Where(user => user.Id == senderUserId).Select(user => user.Email).FirstAsync(cancellationToken);
        var participantIds = await dbContext.ConversationParticipants
            .Where(participant => participant.ConversationId == conversationId && participant.UserId != senderUserId)
            .Select(participant => participant.UserId)
            .ToArrayAsync(cancellationToken);

        foreach (var participantId in participantIds)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                participantId,
                NotificationType.Chat,
                "New message",
                $"New message from {senderEmail}.",
                "Message",
                messageId,
                $"/conversations/{conversationId}"), cancellationToken);
        }
    }

    private async Task<ConversationDto> MapConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations.FirstAsync(item => item.Id == conversationId, cancellationToken);
        var participants = await dbContext.ConversationParticipants
            .Include(participant => participant.User)
            .Where(participant => participant.ConversationId == conversationId)
            .Select(participant => new ConversationParticipantDto(participant.UserId, participant.User.Email, participant.User.FullName, participant.LastReadAt, participant.IsMuted))
            .ToArrayAsync(cancellationToken);
        var lastMessageAt = await dbContext.Messages
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Select(message => (DateTimeOffset?)message.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new ConversationDto(conversation.Id, conversation.Type, conversation.ProjectId, conversation.ApplicationId, conversation.InvestorInterestId, conversation.Title, conversation.CreatedAt, lastMessageAt, participants);
    }

    private async Task<MessageDto> MapMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await dbContext.Messages.Include(item => item.SenderUser).FirstAsync(item => item.Id == messageId, cancellationToken);
        var attachments = await dbContext.MessageAttachments
            .Include(attachment => attachment.File)
            .Where(attachment => attachment.MessageId == messageId)
            .Select(attachment => new MessageAttachmentDto(attachment.Id, attachment.FileId, attachment.File.OriginalFileName, attachment.File.ContentType, attachment.File.SizeInBytes))
            .ToArrayAsync(cancellationToken);
        return new MessageDto(message.Id, message.ConversationId, message.SenderUserId, message.SenderUser.Email, message.IsDeleted ? string.Empty : message.Content, message.IsDeleted, message.CreatedAt, attachments);
    }

    private static void ValidateMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException([new ErrorDetail("Required", "Message content is required", "content")]);
        }

        if (content.Trim().Length > MaxMessageLength)
        {
            throw new ValidationException([new ErrorDetail("MessageTooLong", $"Message content must be at most {MaxMessageLength} characters", "content")]);
        }
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string? reason)
    {
        dbContext.AuditLogs.Add(new AuditLog { ActorUserId = actorUserId, Action = action, ResourceType = resourceType, ResourceId = resourceId, Reason = reason });
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value ?? principal.FindFirst("nameid")?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}

using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Chat.Dtos;

public sealed record CreateConversationRequest(
    ConversationType Type,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    Guid? ProjectId = null,
    Guid? ApplicationId = null,
    Guid? InvestorInterestId = null,
    string? Title = null);

public sealed record ConversationParticipantDto(
    Guid UserId,
    string Email,
    string FullName,
    DateTimeOffset? LastReadAt,
    bool IsMuted);

public sealed record ConversationDto(
    Guid Id,
    ConversationType Type,
    Guid? ProjectId,
    Guid? ApplicationId,
    Guid? InvestorInterestId,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    IReadOnlyCollection<ConversationParticipantDto> Participants);

public sealed record MessageAttachmentDto(
    Guid Id,
    Guid FileId,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes);

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string SenderEmail,
    string Content,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<MessageAttachmentDto> Attachments);

public sealed record MessageListResponse(
    IReadOnlyCollection<MessageDto> Items,
    string? NextCursor);

public sealed record SendMessageRequest(
    string Content,
    IReadOnlyCollection<Guid>? AttachmentFileIds = null);

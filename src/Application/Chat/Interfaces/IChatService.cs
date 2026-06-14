using System.Security.Claims;
using StartupConnect.Application.Chat.Dtos;

namespace StartupConnect.Application.Chat.Interfaces;

public interface IChatService
{
    Task<ConversationDto> CreateConversationAsync(ClaimsPrincipal principal, CreateConversationRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ConversationDto>> GetMyConversationsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ConversationDto> GetConversationAsync(ClaimsPrincipal principal, Guid conversationId, CancellationToken cancellationToken);

    Task<MessageListResponse> GetMessagesAsync(ClaimsPrincipal principal, Guid conversationId, string? beforeCursor, int pageSize, CancellationToken cancellationToken);

    Task<MessageDto> SendMessageAsync(ClaimsPrincipal principal, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken);

    Task MarkReadAsync(ClaimsPrincipal principal, Guid conversationId, CancellationToken cancellationToken);

    Task DeleteMessageAsync(ClaimsPrincipal principal, Guid messageId, CancellationToken cancellationToken);
}

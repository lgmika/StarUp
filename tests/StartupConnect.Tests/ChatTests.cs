using StartupConnect.Application.Chat.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ChatDtoTests
{
    [Fact]
    public void ConversationDto_Should_Represent_Type_And_Participants()
    {
        var participant = new ConversationParticipantDto(
            Guid.NewGuid(),
            "member@example.com",
            "Member",
            null,
            false);

        var conversation = new ConversationDto(
            Guid.NewGuid(),
            ConversationType.Project,
            Guid.NewGuid(),
            null,
            null,
            "Project chat",
            DateTimeOffset.UtcNow,
            null,
            [participant]);

        Assert.Equal(ConversationType.Project, conversation.Type);
        Assert.Single(conversation.Participants);
    }
}

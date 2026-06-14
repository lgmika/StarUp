using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class NotificationDtoTests
{
    [Fact]
    public void NotificationDto_Should_Expose_Read_State_And_Action_Url()
    {
        var readAt = DateTimeOffset.UtcNow;
        var notification = new NotificationDto(
            Guid.NewGuid(),
            NotificationType.System,
            "NDA accepted",
            "A user accepted the project NDA.",
            "NdaAgreement",
            Guid.NewGuid(),
            "/projects/abc/nda",
            true,
            readAt,
            DateTimeOffset.UtcNow);

        Assert.True(notification.IsRead);
        Assert.Equal(readAt, notification.ReadAt);
        Assert.Equal("/projects/abc/nda", notification.ActionUrl);
    }
}

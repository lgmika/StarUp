using StartupConnect.Application.Activities.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ActivityTests
{
    [Fact]
    public void ActivityListResponse_Should_Represent_Paginated_Feed()
    {
        var activity = new ActivityDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "StartupConnect",
            Guid.NewGuid(),
            "Founder",
            ActivityType.ProjectPublished,
            ActivityVisibility.Public,
            "Project published",
            "Project StartupConnect is now published.",
            "Project",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var response = new ActivityListResponse([activity], 1, 1, 20);

        Assert.Single(response.Items);
        Assert.Equal(ActivityType.ProjectPublished, response.Items.First().Type);
        Assert.Equal(ActivityVisibility.Public, response.Items.First().Visibility);
        Assert.Equal(20, response.PageSize);
    }
}

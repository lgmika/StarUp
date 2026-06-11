using StartupConnect.Application.Moderation.Dtos;

namespace StartupConnect.Tests;

public sealed class ModeratorDtoTests
{
    [Fact]
    public void DashboardDto_Should_Report_Pending_Counts()
    {
        var dashboard = new ModeratorDashboardDto(
            PendingProjects: 2,
            PublishedProjects: 3,
            RejectedProjects: 1,
            HiddenProjects: 0,
            PendingReports: 4);

        Assert.Equal(2, dashboard.PendingProjects);
        Assert.Equal(4, dashboard.PendingReports);
    }
}


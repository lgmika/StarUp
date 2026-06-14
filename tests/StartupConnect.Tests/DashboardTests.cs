using StartupConnect.Application.Dashboards;
using StartupConnect.Application.Dashboards.Dtos;

namespace StartupConnect.Tests;

public sealed class DashboardTests
{
    [Fact]
    public void DashboardMetrics_Should_Calculate_Profile_Completion_Percent()
    {
        var percent = DashboardMetrics.CompletionPercent(completedFields: 3, totalFields: 6);

        Assert.Equal(50, percent);
    }

    [Fact]
    public void DashboardMetrics_Should_Calculate_Conversion_Rate()
    {
        var conversion = DashboardMetrics.ConversionRate(converted: 2, total: 8);

        Assert.Equal(0.25, conversion);
    }

    [Fact]
    public void UserDashboardDto_Should_Expose_Aggregates()
    {
        var dashboard = new UserDashboardDto(
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow,
            420,
            4,
            [new CountByStatusDto("Pending", 3)],
            1,
            2,
            5,
            80);

        Assert.Equal(4, dashboard.Applications);
        Assert.Equal(420, dashboard.TimezoneOffsetMinutes);
        Assert.Equal(80, dashboard.ProfileCompletionPercent);
    }
}

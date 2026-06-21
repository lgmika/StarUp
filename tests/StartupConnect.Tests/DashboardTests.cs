using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using StartupConnect.Application.Dashboards;
using StartupConnect.Application.Dashboards.Dtos;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.Persistence;

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

    [Fact]
    public void ProjectView_Model_Should_Enforce_Daily_Viewer_Deduplication()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=startupconnect_model_test")
            .Options;
        using var dbContext = new AppDbContext(options);

        var entity = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProjectView));
        Assert.NotNull(entity);

        var uniqueIndexes = entity.GetIndexes()
            .Where(index => index.IsUnique)
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToArray();

        Assert.Contains("ProjectId,ViewerUserId,ViewedOn", uniqueIndexes);
        Assert.Contains("ProjectId,VisitorId,ViewedOn", uniqueIndexes);
        Assert.Contains(entity.GetCheckConstraints(), constraint => constraint.Name == "CK_project_views_viewer");
    }
}

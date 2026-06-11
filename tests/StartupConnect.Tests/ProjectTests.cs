using StartupConnect.Application.Projects.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ProjectDtoTests
{
    [Fact]
    public void ProjectSummaryDto_Should_Represent_Project_Status_And_Visibility()
    {
        var summary = new ProjectSummaryDto(
            Guid.NewGuid(),
            "StartupConnect",
            "startupconnect",
            "Connect startup teams",
            ProjectStatus.Draft,
            ProjectStage.MVP,
            ProjectVisibility.Public,
            true,
            DateTimeOffset.UtcNow);

        Assert.Equal(ProjectStatus.Draft, summary.Status);
        Assert.Equal(ProjectVisibility.Public, summary.Visibility);
    }
}


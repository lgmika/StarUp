using StartupConnect.Application.Applications.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ApplicationDtoTests
{
    [Fact]
    public void ApplicationDto_Should_Represent_Status()
    {
        var application = new ApplicationDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Project",
            Guid.NewGuid(),
            "member@example.com",
            "Member",
            null,
            null,
            "I want to help.",
            ApplicationStatus.Pending,
            null,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal(ApplicationStatus.Pending, application.Status);
    }
}


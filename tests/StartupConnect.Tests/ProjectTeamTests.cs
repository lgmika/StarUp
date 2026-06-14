using StartupConnect.Application.ProjectTeams.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ProjectTeamDtoTests
{
    [Fact]
    public void ProjectInvitationDto_Should_Represent_Status_And_Role()
    {
        var invitation = new ProjectInvitationDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "member@example.com",
            ProjectMemberRole.Member,
            "Join us",
            ProjectInvitationStatus.Pending,
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal(ProjectInvitationStatus.Pending, invitation.Status);
        Assert.Equal(ProjectMemberRole.Member, invitation.Role);
    }
}

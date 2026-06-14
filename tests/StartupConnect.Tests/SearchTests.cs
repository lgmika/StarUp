using StartupConnect.Application.Search;
using StartupConnect.Application.Search.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class SearchTests
{
    [Fact]
    public void SearchAccessPolicy_Should_Block_Private_Project_From_Anonymous_Search()
    {
        var canSee = SearchAccessPolicy.CanSeeProject(
            ProjectStatus.Published,
            ProjectVisibility.NdaRequired,
            isOwner: false,
            isMember: false,
            hasAccessGrant: false);

        Assert.False(canSee);
    }

    [Fact]
    public void SearchAccessPolicy_Should_Allow_Project_With_Access_Grant()
    {
        var canSee = SearchAccessPolicy.CanSeeProject(
            ProjectStatus.Published,
            ProjectVisibility.NdaRequired,
            isOwner: false,
            isMember: false,
            hasAccessGrant: true);

        Assert.True(canSee);
    }

    [Fact]
    public void SearchResultPage_Should_Represent_Ranked_Project_Results()
    {
        var item = new ProjectSearchItemDto(
            Guid.NewGuid(),
            "StartupConnect",
            "startupconnect",
            "Connect founders and members",
            ProjectStatus.Published,
            ProjectStage.MVP,
            ProjectVisibility.Public,
            true,
            DateTimeOffset.UtcNow,
            0.91f);

        var page = new SearchResultPage<ProjectSearchItemDto>([item], 1, 1, 20);

        Assert.Single(page.Items);
        Assert.Equal(0.91f, page.Items.First().Rank);
    }
}

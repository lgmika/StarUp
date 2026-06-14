using StartupConnect.Application.Recommendations;
using StartupConnect.Application.Recommendations.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class RecommendationTests
{
    [Fact]
    public void RecommendationScoring_Should_Calculate_Skill_Match_Ratio()
    {
        var score = RecommendationScoring.SkillMatchScore(matchedSkills: 2, requiredSkills: 4);

        Assert.Equal(25, score);
    }

    [Fact]
    public void ProjectRecommendationDto_Should_Expose_Scoring_Breakdown()
    {
        var recommendation = new ProjectRecommendationDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "StartupConnect",
            "Founder/member matching platform",
            ProjectStage.MVP,
            ProjectVisibility.Public,
            70,
            [new RecommendationBreakdownItemDto("skillMatch", 50, "All required skills match.")]);

        Assert.Equal(70, recommendation.Score);
        Assert.Single(recommendation.Breakdown);
        Assert.Equal("skillMatch", recommendation.Breakdown.First().Key);
    }
}

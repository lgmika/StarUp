using StartupConnect.Application.AI.Dtos;

namespace StartupConnect.Tests;

public sealed class AIDtoTests
{
    [Fact]
    public void AIReviewDto_Should_Carry_Score_And_Risk_Flags()
    {
        var review = new AIReviewDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            82,
            ["Target market is missing."],
            ["Summary is short."],
            ["Add measurable MVP goals."],
            "Mock review",
            DateTimeOffset.UtcNow);

        Assert.Equal(82, review.QualityScore);
        Assert.Single(review.RiskFlags);
    }
}


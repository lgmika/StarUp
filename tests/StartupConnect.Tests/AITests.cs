using StartupConnect.Application.AI.Dtos;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.AI;

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

    [Fact]
    public async Task MockAIProvider_Should_Generate_Provider_Compatible_Output()
    {
        var provider = new MockAIProvider();
        var project = new AIProjectContext(
            Guid.NewGuid(),
            "StartupConnect",
            "A platform for matching founders, teammates, advisors, and investors.",
            "Startup teams struggle to find trusted collaborators.",
            "Use structured project profiles, applications, and investor access workflows.",
            string.Empty,
            string.Empty,
            "MVP",
            []);

        var suggestions = await provider.GenerateProjectSuggestionsAsync(project, CancellationToken.None);
        var review = await provider.ReviewProjectAsync(project, CancellationToken.None);

        Assert.Equal("Mock", provider.Name);
        Assert.Equal(3, suggestions.Count);
        Assert.InRange(review.QualityScore, 0, 100);
        Assert.NotEmpty(review.MissingInformation);
    }

    [Fact]
    public void AIRequest_Should_Persist_Generated_Response()
    {
        var request = new AIRequest
        {
            PromptSnapshot = "Project snapshot",
            ResponseSnapshot = "Generated investor summary"
        };

        Assert.Equal("Generated investor summary", request.ResponseSnapshot);
    }
}

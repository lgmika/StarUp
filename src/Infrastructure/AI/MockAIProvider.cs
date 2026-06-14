namespace StartupConnect.Infrastructure.AI;

public sealed class MockAIProvider : IAIProvider
{
    public string Name => "Mock";

    public Task<IReadOnlyCollection<AIProjectSuggestionResult>> GenerateProjectSuggestionsAsync(
        AIProjectContext project,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AIProjectSuggestionResult> result =
        [
            new("Clarify the target user", "TargetMarket", $"Describe exactly who has the painful problem behind '{project.Title}' and why they need this now."),
            new("Strengthen the solution", "Solution", "Add the key workflow, differentiator, and first measurable outcome the MVP will deliver."),
            new("Improve recruiting needs", "RequiredRoles", "List concrete role responsibilities, weekly time expectation, and must-have skills for each needed teammate.")
        ];

        return Task.FromResult(result);
    }

    public Task<AIProjectReviewResult> ReviewProjectAsync(
        AIProjectContext project,
        CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(project.TargetMarket)) missing.Add("Target market is missing.");
        if (string.IsNullOrWhiteSpace(project.BusinessModel)) missing.Add("Business model is missing.");
        if (project.RequiredRoles.Count == 0) missing.Add("Required team roles are missing.");

        var risks = new List<string>();
        if (project.Summary.Length < 80) risks.Add("Project summary may be too short for reviewers.");

        var suggestions = new[]
        {
            "Explain the problem with one concrete customer scenario.",
            "Add measurable MVP success criteria.",
            "Describe what kind of teammate or investor would be most helpful now."
        };

        var score = Math.Clamp(85 - missing.Count * 10 - risks.Count * 7, 35, 95);
        return Task.FromResult(new AIProjectReviewResult(
            score,
            missing,
            risks,
            suggestions,
            "Mock review only: use this as a checklist, not as an approval decision."));
    }

    public Task<AITextResult> GenerateCoverLetterAsync(string applicationSnapshot, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AITextResult("Mock cover letter: I am excited about this project because it matches my skills and I can contribute consistently to the MVP goals."));
    }

    public Task<AITextResult> GenerateInvestorSummaryAsync(AIProjectContext project, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AITextResult($"Mock investor summary: {project.Title} is at {project.Stage} stage, focused on {project.Summary}. Key review areas: market clarity, MVP traction, team gaps, and funding use."));
    }
}

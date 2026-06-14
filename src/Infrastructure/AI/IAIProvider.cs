namespace StartupConnect.Infrastructure.AI;

public interface IAIProvider
{
    string Name { get; }

    Task<IReadOnlyCollection<AIProjectSuggestionResult>> GenerateProjectSuggestionsAsync(
        AIProjectContext project,
        CancellationToken cancellationToken);

    Task<AIProjectReviewResult> ReviewProjectAsync(
        AIProjectContext project,
        CancellationToken cancellationToken);

    Task<AITextResult> GenerateCoverLetterAsync(
        string applicationSnapshot,
        CancellationToken cancellationToken);

    Task<AITextResult> GenerateInvestorSummaryAsync(
        AIProjectContext project,
        CancellationToken cancellationToken);
}

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace StartupConnect.Infrastructure.AI;

public sealed class OllamaAIProvider(IOptions<AIOptions> options) : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AIOptions _options = options.Value;

    public string Name => "Ollama";

    public async Task<IReadOnlyCollection<AIProjectSuggestionResult>> GenerateProjectSuggestionsAsync(
        AIProjectContext project,
        CancellationToken cancellationToken)
    {
        var prompt = string.Join('\n',
            "Return JSON only:",
            """{"items":[{"title":"...","targetField":"...","content":"..."}]}""",
            string.Empty,
            "Create exactly 3 concise improvement suggestions for this startup project.",
            "Project:",
            JsonSerializer.Serialize(project, JsonOptions));

        var result = await GenerateJsonAsync<SuggestionEnvelope>(prompt, cancellationToken);
        return result.Items.Take(3).ToArray();
    }

    public async Task<AIProjectReviewResult> ReviewProjectAsync(
        AIProjectContext project,
        CancellationToken cancellationToken)
    {
        var prompt = string.Join('\n',
            "Return JSON only:",
            """{"qualityScore":0,"missingInformation":[],"riskFlags":[],"suggestions":[],"summary":"..."}""",
            string.Empty,
            "Review this startup project for readiness. Score from 0 to 100.",
            "Project:",
            JsonSerializer.Serialize(project, JsonOptions));

        return await GenerateJsonAsync<AIProjectReviewResult>(prompt, cancellationToken);
    }

    public async Task<AITextResult> GenerateCoverLetterAsync(
        string applicationSnapshot,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                     Write a concise, professional project application cover letter.
                     Keep it under 180 words. Do not invent facts beyond the snapshot.

                     Application snapshot:
                     {applicationSnapshot}
                     """;

        return new AITextResult(await GenerateTextAsync(prompt, false, cancellationToken));
    }

    public async Task<AITextResult> GenerateInvestorSummaryAsync(
        AIProjectContext project,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
                     Write a concise investor-facing summary for this startup project.
                     Include market, solution, stage, key risks, and next diligence questions.
                     Keep it under 220 words.

                     Project:
                     {JsonSerializer.Serialize(project, JsonOptions)}
                     """;

        return new AITextResult(await GenerateTextAsync(prompt, false, cancellationToken));
    }

    private async Task<T> GenerateJsonAsync<T>(string prompt, CancellationToken cancellationToken)
    {
        var text = await GenerateTextAsync(prompt, true, cancellationToken);
        return JsonSerializer.Deserialize<T>(StripCodeFence(text), JsonOptions)
            ?? throw new InvalidOperationException("Ollama returned an empty JSON response.");
    }

    private async Task<string> GenerateTextAsync(string prompt, bool jsonMode, CancellationToken cancellationToken)
    {
        var settings = _options.Ollama;
        using var client = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
        };

        var request = new Dictionary<string, object?>
        {
            ["model"] = settings.Model,
            ["prompt"] = prompt,
            ["system"] = "You are StartupConnect's backend AI assistant. Be concise, factual, and follow the requested output format.",
            ["stream"] = false
        };

        if (jsonMode)
        {
            request["format"] = "json";
        }

        using var response = await client.PostAsJsonAsync("api/generate", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        if (string.IsNullOrWhiteSpace(payload.Response))
        {
            throw new InvalidOperationException("Ollama returned empty generated text.");
        }

        return payload.Response.Trim();
    }

    private static string StripCodeFence(string text)
    {
        var clean = text.Trim();
        if (!clean.StartsWith("```", StringComparison.Ordinal))
        {
            return clean;
        }

        var firstLineEnd = clean.IndexOf('\n');
        if (firstLineEnd < 0)
        {
            return clean;
        }

        clean = clean[(firstLineEnd + 1)..].Trim();
        if (clean.EndsWith("```", StringComparison.Ordinal))
        {
            clean = clean[..^3].Trim();
        }

        return clean;
    }

    private sealed record OllamaGenerateResponse(string Response);

    private sealed record SuggestionEnvelope(IReadOnlyCollection<AIProjectSuggestionResult> Items);
}

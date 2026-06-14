using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Search.Dtos;

public sealed record ProjectSearchQuery(
    string? Keyword = null,
    ProjectStatus? Status = null,
    ProjectStage? Stage = null,
    string? RequiredRole = null,
    Guid? RequiredSkillId = null,
    string? Location = null,
    bool? Remote = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20);

public sealed record MemberSearchQuery(
    string? Keyword = null,
    Guid? SkillId = null,
    int? MinYearsOfExperience = null,
    string? Location = null,
    bool VerifiedOnly = false,
    int Page = 1,
    int PageSize = 20);

public sealed record InvestorSearchQuery(
    string? Keyword = null,
    decimal? MinTicketSize = null,
    decimal? MaxTicketSize = null,
    int Page = 1,
    int PageSize = 20);

public sealed record SearchSuggestionQuery(string? Keyword = null, int Limit = 10);

public sealed record ProjectSearchItemDto(
    Guid Id,
    string Title,
    string Slug,
    string Summary,
    ProjectStatus Status,
    ProjectStage Stage,
    ProjectVisibility Visibility,
    bool IsRecruiting,
    DateTimeOffset CreatedAt,
    float Rank);

public sealed record MemberSearchItemDto(
    Guid UserId,
    string FullName,
    string Headline,
    string? Location,
    IReadOnlyCollection<string> Skills,
    float Rank);

public sealed record InvestorSearchItemDto(
    Guid UserId,
    string DisplayName,
    string? OrganizationName,
    string? InvestmentFocus,
    decimal? MinTicketSize,
    decimal? MaxTicketSize,
    float Rank);

public sealed record SearchSuggestionDto(
    string Type,
    Guid? Id,
    string Label,
    string? Description);

public sealed record SearchResultPage<T>(
    IReadOnlyCollection<T> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record SearchSuggestionsResponse(IReadOnlyCollection<SearchSuggestionDto> Items);

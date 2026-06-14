using System.Security.Claims;
using StartupConnect.Application.Search.Dtos;

namespace StartupConnect.Application.Search.Interfaces;

public interface ISearchService
{
    Task<SearchResultPage<ProjectSearchItemDto>> SearchProjectsAsync(
        ClaimsPrincipal? principal,
        ProjectSearchQuery query,
        CancellationToken cancellationToken);

    Task<SearchResultPage<MemberSearchItemDto>> SearchMembersAsync(
        ClaimsPrincipal? principal,
        MemberSearchQuery query,
        CancellationToken cancellationToken);

    Task<SearchResultPage<InvestorSearchItemDto>> SearchInvestorsAsync(
        ClaimsPrincipal principal,
        InvestorSearchQuery query,
        CancellationToken cancellationToken);

    Task<SearchSuggestionsResponse> GetSuggestionsAsync(
        ClaimsPrincipal? principal,
        SearchSuggestionQuery query,
        CancellationToken cancellationToken);
}

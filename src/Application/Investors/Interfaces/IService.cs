using System.Security.Claims;
using StartupConnect.Application.Investors.Dtos;

namespace StartupConnect.Application.Investors.Interfaces;

public interface IInvestorService
{
    Task<InvestorProfileDto> GetMyProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<InvestorProfileDto> CreateProfileAsync(ClaimsPrincipal principal, UpsertInvestorProfileRequest request, CancellationToken cancellationToken);

    Task<InvestorProfileDto> UpdateProfileAsync(ClaimsPrincipal principal, UpsertInvestorProfileRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InvestorProjectDiscoveryDto>> GetInvestorProjectsAsync(ClaimsPrincipal principal, string? search, CancellationToken cancellationToken);

    Task<InvestorInterestDto> CreateInterestAsync(ClaimsPrincipal principal, Guid projectId, CreateInvestorInterestRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InvestorInterestDto>> GetMyInterestsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InvestorInterestDto>> GetProjectInterestsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<InvestorInterestDto> AcceptInterestAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, InvestorInterestDecisionRequest request, CancellationToken cancellationToken);

    Task<InvestorInterestDto> RejectInterestAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, InvestorInterestDecisionRequest request, CancellationToken cancellationToken);

    Task<InvestorInterestDto> RequestMoreInfoAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, InvestorInterestDecisionRequest request, CancellationToken cancellationToken);

    Task<InvestorInterestDto> WithdrawInterestAsync(ClaimsPrincipal principal, Guid projectId, Guid interestId, CancellationToken cancellationToken);
}


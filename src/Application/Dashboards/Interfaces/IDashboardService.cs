using System.Security.Claims;
using StartupConnect.Application.Dashboards.Dtos;

namespace StartupConnect.Application.Dashboards.Interfaces;

public interface IDashboardService
{
    Task<UserDashboardDto> GetMyDashboardAsync(
        ClaimsPrincipal principal,
        DashboardQuery query,
        CancellationToken cancellationToken);

    Task<FounderProjectDashboardDto> GetFounderProjectDashboardAsync(
        ClaimsPrincipal principal,
        Guid projectId,
        DashboardQuery query,
        CancellationToken cancellationToken);

    Task<InvestorDashboardDto> GetInvestorDashboardAsync(
        ClaimsPrincipal principal,
        DashboardQuery query,
        CancellationToken cancellationToken);
}

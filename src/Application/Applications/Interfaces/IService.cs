using System.Security.Claims;
using StartupConnect.Application.Applications.Dtos;

namespace StartupConnect.Application.Applications.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> ApplyAsync(ClaimsPrincipal principal, Guid projectId, ApplyProjectRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ApplicationDto>> GetProjectApplicationsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<ApplicationDetailDto> GetProjectApplicationAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ApplicationDto>> GetMyApplicationsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task WithdrawAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken);

    Task<ApplicationDto> ShortlistAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken);

    Task<ApplicationDto> InterviewAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken);

    Task<ApplicationDto> AcceptAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken);

    Task<ApplicationDto> RejectAsync(ClaimsPrincipal principal, Guid projectId, Guid applicationId, ApplicationDecisionRequest request, CancellationToken cancellationToken);
}


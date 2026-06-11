using System.Security.Claims;
using StartupConnect.Application.Moderation.Dtos;

namespace StartupConnect.Application.Moderation.Interfaces;

public interface IModeratorService
{
    Task<ModeratorDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ModeratorProjectQueueItemDto>> GetPendingProjectsAsync(CancellationToken cancellationToken);

    Task<ModeratorProjectDetailDto> GetProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task ApproveProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken);

    Task RequestImprovementAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken);

    Task RejectProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken);

    Task HideProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken);

    Task RestoreProjectAsync(ClaimsPrincipal principal, Guid projectId, ModerationDecisionRequest request, CancellationToken cancellationToken);
}


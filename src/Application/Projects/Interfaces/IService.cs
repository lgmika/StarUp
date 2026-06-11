using System.Security.Claims;
using StartupConnect.Application.Projects.Dtos;

namespace StartupConnect.Application.Projects.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyCollection<ProjectSummaryDto>> ListProjectsAsync(string? search, CancellationToken cancellationToken);

    Task<ProjectDetailDto> GetProjectAsync(ClaimsPrincipal? principal, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectDetailDto> CreateDraftAsync(ClaimsPrincipal principal, CreateProjectDraftRequest request, CancellationToken cancellationToken);

    Task<ProjectDetailDto> UpdateProjectAsync(ClaimsPrincipal principal, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken);

    Task DeleteProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task SubmitReviewAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task CloseProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectSummaryDto>> GetOwnedProjectsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectSummaryDto>> GetJoinedProjectsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectVersionDto>> GetVersionsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task SaveProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task UnsaveProjectAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectSummaryDto>> GetSavedProjectsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}


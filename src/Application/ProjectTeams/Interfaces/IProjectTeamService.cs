using System.Security.Claims;
using StartupConnect.Application.ProjectTeams.Dtos;

namespace StartupConnect.Application.ProjectTeams.Interfaces;

public interface IProjectTeamService
{
    Task<IReadOnlyCollection<ProjectMemberDto>> GetMembersAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectMemberDto> GetMemberAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken);

    Task<ProjectMemberDto> UpdateMemberAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, UpdateProjectMemberRequest request, CancellationToken cancellationToken);

    Task RemoveMemberAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken);

    Task<ProjectInvitationDto> CreateInvitationAsync(ClaimsPrincipal principal, Guid projectId, CreateProjectInvitationRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectInvitationDto>> GetInvitationsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectInvitationDto> AcceptInvitationAsync(ClaimsPrincipal principal, Guid invitationId, CancellationToken cancellationToken);

    Task<ProjectInvitationDto> RejectInvitationAsync(ClaimsPrincipal principal, Guid invitationId, CancellationToken cancellationToken);

    Task<ProjectInvitationDto> CancelInvitationAsync(ClaimsPrincipal principal, Guid invitationId, CancellationToken cancellationToken);

    Task<ProjectOwnershipTransferDto> CreateOwnershipTransferAsync(ClaimsPrincipal principal, Guid projectId, TransferOwnershipRequest request, CancellationToken cancellationToken);

    Task<ProjectOwnershipTransferDto> AcceptOwnershipTransferAsync(ClaimsPrincipal principal, Guid projectId, AcceptOwnershipTransferRequest request, CancellationToken cancellationToken);

    Task<ProjectMemberDto> PromoteCoFounderAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken);

    Task<ProjectMemberDto> RemoveCoFounderAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken);

    Task LeaveAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);
}

using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Email.Interfaces;
using StartupConnect.Application.Email.Models;
using StartupConnect.Application.ProjectTeams.Dtos;
using StartupConnect.Application.ProjectTeams.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Auth;
using StartupConnect.Infrastructure.Email;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.ProjectTeams;

public sealed class ProjectTeamService(
    AppDbContext dbContext,
    IEmailService emailService,
    SecureTokenGenerator tokenGenerator) : IProjectTeamService
{
    public async Task<IReadOnlyCollection<ProjectMemberDto>> GetMembersAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanViewProjectAsync(projectId, userId, cancellationToken);

        return await QueryMembers(projectId)
            .OrderBy(member => member.Role)
            .ThenBy(member => member.JoinedAt)
            .Select(member => MapMember(member))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ProjectMemberDto> GetMemberAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        await EnsureCanViewProjectAsync(projectId, userId, cancellationToken);
        return MapMember(await GetMemberAsync(projectId, memberId, cancellationToken));
    }

    public async Task<ProjectMemberDto> UpdateMemberAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, UpdateProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        await EnsureCanManageProjectAsync(projectId, actorUserId, cancellationToken);
        var member = await GetMemberAsync(projectId, memberId, cancellationToken);

        if (member.Role == ProjectMemberRole.Founder || request.Role == ProjectMemberRole.Founder)
        {
            throw new ApiException("Founder role can only change through ownership transfer", HttpStatusCode.BadRequest);
        }

        var previous = member.Role;
        member.Role = request.Role;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        AddHistory(projectId, member.Id, member.UserId, actorUserId, "RoleUpdated", previous, request.Role, request.Reason);
        AddAudit(actorUserId, "ProjectMember.Update", "ProjectMember", member.Id, request.Reason);
        AddActivity(projectId, actorUserId, ActivityType.MemberRoleChanged, ActivityVisibility.MembersOnly, "Member role changed", $"A member role changed from {previous} to {request.Role}.", "ProjectMember", member.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapMember(member);
    }

    public async Task RemoveMemberAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, actorUserId, cancellationToken);
        var member = await GetMemberAsync(projectId, memberId, cancellationToken);
        if (member.Role == ProjectMemberRole.Founder)
        {
            throw new ApiException("Founder cannot be removed", HttpStatusCode.BadRequest);
        }

        member.IsActive = false;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await RemoveAccessGrantIfAnyAsync(projectId, member.UserId, cancellationToken);
        AddHistory(projectId, member.Id, member.UserId, actorUserId, "Removed", member.Role, null, "Removed from project");
        AddAudit(actorUserId, "ProjectMember.Remove", "ProjectMember", member.Id, "Removed from project");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectInvitationDto> CreateInvitationAsync(ClaimsPrincipal principal, Guid projectId, CreateProjectInvitationRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, actorUserId, cancellationToken);
        ValidateEmail(request.Email);
        if (request.Role == ProjectMemberRole.Founder)
        {
            throw new ApiException("Founder role cannot be invited directly", HttpStatusCode.BadRequest);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var invitedUser = await dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        if (invitedUser is not null && await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == invitedUser.Id && member.IsActive, cancellationToken))
        {
            throw new ApiException("User is already an active project member", HttpStatusCode.Conflict);
        }

        var duplicate = await dbContext.ProjectInvitations.AnyAsync(invitation =>
            invitation.ProjectId == projectId &&
            invitation.Status == ProjectInvitationStatus.Pending &&
            invitation.ExpiresAt > DateTimeOffset.UtcNow &&
            (invitation.Email.ToUpper() == normalizedEmail || (invitedUser != null && invitation.InvitedUserId == invitedUser.Id)),
            cancellationToken);
        if (duplicate)
        {
            throw new ApiException("A pending invitation already exists for this user or email", HttpStatusCode.Conflict);
        }

        var invitation = new ProjectInvitation
        {
            ProjectId = projectId,
            InvitedByUserId = actorUserId,
            InvitedUserId = invitedUser?.Id,
            Email = request.Email.Trim(),
            Role = request.Role,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        dbContext.ProjectInvitations.Add(invitation);
        AddAudit(actorUserId, "ProjectInvitation.Create", "Project", projectId, request.Email);
        await dbContext.SaveChangesAsync(cancellationToken);

        var projectName = await dbContext.Projects.Where(project => project.Id == projectId).Select(project => project.Title).FirstAsync(cancellationToken);
        await emailService.SendProjectInvitationAsync(invitation.Email, new ProjectInvitationEmailModel(projectName, "StartupConnect team", request.Role.ToString(), $"/project-invitations/{invitation.Id}", invitation.Message), cancellationToken);

        return MapInvitation(invitation);
    }

    public async Task<IReadOnlyCollection<ProjectInvitationDto>> GetInvitationsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        await EnsureCanManageProjectAsync(projectId, actorUserId, cancellationToken);
        return await dbContext.ProjectInvitations
            .Where(invitation => invitation.ProjectId == projectId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => MapInvitation(invitation))
            .ToArrayAsync(cancellationToken);
    }

    public Task<ProjectInvitationDto> AcceptInvitationAsync(ClaimsPrincipal principal, Guid invitationId, CancellationToken cancellationToken)
    {
        return RespondInvitationAsync(principal, invitationId, ProjectInvitationStatus.Accepted, cancellationToken);
    }

    public Task<ProjectInvitationDto> RejectInvitationAsync(ClaimsPrincipal principal, Guid invitationId, CancellationToken cancellationToken)
    {
        return RespondInvitationAsync(principal, invitationId, ProjectInvitationStatus.Rejected, cancellationToken);
    }

    public async Task<ProjectInvitationDto> CancelInvitationAsync(ClaimsPrincipal principal, Guid invitationId, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        var invitation = await GetInvitationAsync(invitationId, cancellationToken);
        await EnsureCanManageProjectAsync(invitation.ProjectId, actorUserId, cancellationToken);
        EnsureInvitationPending(invitation);
        invitation.Status = ProjectInvitationStatus.Cancelled;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        invitation.UpdatedAt = DateTimeOffset.UtcNow;
        AddAudit(actorUserId, "ProjectInvitation.Cancel", "ProjectInvitation", invitation.Id, "Cancelled");
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapInvitation(invitation);
    }

    public async Task<ProjectOwnershipTransferDto> CreateOwnershipTransferAsync(ClaimsPrincipal principal, Guid projectId, TransferOwnershipRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        await EnsureFounderAsync(projectId, actorUserId, cancellationToken);
        var targetMember = await dbContext.ProjectMembers.FirstOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == request.ToUserId && member.IsActive, cancellationToken)
            ?? throw new ApiException("Ownership recipient must be an active project member", HttpStatusCode.BadRequest);
        if (targetMember.Role == ProjectMemberRole.Founder)
        {
            throw new ApiException("User is already the founder", HttpStatusCode.BadRequest);
        }

        var token = tokenGenerator.CreateToken();
        var transfer = new ProjectOwnershipTransfer
        {
            ProjectId = projectId,
            FromUserId = actorUserId,
            ToUserId = request.ToUserId,
            TokenHash = tokenGenerator.HashToken(token),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };
        dbContext.ProjectOwnershipTransfers.Add(transfer);
        AddAudit(actorUserId, "ProjectOwnershipTransfer.Create", "Project", projectId, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTransfer(transfer, token);
    }

    public async Task<ProjectOwnershipTransferDto> AcceptOwnershipTransferAsync(ClaimsPrincipal principal, Guid projectId, AcceptOwnershipTransferRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var tokenHash = tokenGenerator.HashToken(request.Token);
        var transfer = await dbContext.ProjectOwnershipTransfers.FirstOrDefaultAsync(item =>
            item.ProjectId == projectId &&
            item.ToUserId == userId &&
            item.TokenHash == tokenHash &&
            item.Status == ProjectOwnershipTransferStatus.Pending,
            cancellationToken)
            ?? throw new ApiException("Ownership transfer not found", HttpStatusCode.NotFound);
        if (transfer.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            transfer.Status = ProjectOwnershipTransferStatus.Expired;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new ApiException("Ownership transfer has expired", HttpStatusCode.BadRequest);
        }

        var oldFounder = await dbContext.ProjectMembers.FirstAsync(member => member.ProjectId == projectId && member.UserId == transfer.FromUserId && member.IsActive, cancellationToken);
        var newFounder = await dbContext.ProjectMembers.FirstAsync(member => member.ProjectId == projectId && member.UserId == transfer.ToUserId && member.IsActive, cancellationToken);
        var previousRecipientRole = newFounder.Role;
        oldFounder.Role = ProjectMemberRole.CoFounder;
        newFounder.Role = ProjectMemberRole.Founder;
        transfer.Status = ProjectOwnershipTransferStatus.Accepted;
        transfer.AcceptedAt = DateTimeOffset.UtcNow;
        transfer.UpdatedAt = DateTimeOffset.UtcNow;
        AddHistory(projectId, oldFounder.Id, oldFounder.UserId, userId, "OwnershipTransferredFrom", ProjectMemberRole.Founder, ProjectMemberRole.CoFounder, "Ownership accepted");
        AddHistory(projectId, newFounder.Id, newFounder.UserId, userId, "OwnershipTransferredTo", previousRecipientRole, ProjectMemberRole.Founder, "Ownership accepted");
        AddAudit(userId, "ProjectOwnershipTransfer.Accept", "Project", projectId, "Ownership accepted");
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTransfer(transfer);
    }

    public Task<ProjectMemberDto> PromoteCoFounderAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        return UpdateMemberAsync(principal, projectId, memberId, new UpdateProjectMemberRequest(ProjectMemberRole.CoFounder, "Promoted to co-founder"), cancellationToken);
    }

    public Task<ProjectMemberDto> RemoveCoFounderAsync(ClaimsPrincipal principal, Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        return UpdateMemberAsync(principal, projectId, memberId, new UpdateProjectMemberRequest(ProjectMemberRole.Member, "Removed co-founder role"), cancellationToken);
    }

    public async Task LeaveAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var member = await dbContext.ProjectMembers.FirstOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == userId && item.IsActive, cancellationToken)
            ?? throw new ApiException("Project membership not found", HttpStatusCode.NotFound);
        if (member.Role == ProjectMemberRole.Founder)
        {
            throw new ApiException("Founder must transfer ownership before leaving", HttpStatusCode.BadRequest);
        }

        member.IsActive = false;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await RemoveAccessGrantIfAnyAsync(projectId, userId, cancellationToken);
        AddHistory(projectId, member.Id, userId, userId, "Left", member.Role, null, "Member left project");
        AddAudit(userId, "ProjectMember.Leave", "Project", projectId, "Member left project");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProjectInvitationDto> RespondInvitationAsync(ClaimsPrincipal principal, Guid invitationId, ProjectInvitationStatus status, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var userEmail = await dbContext.Users.Where(user => user.Id == userId).Select(user => user.Email).FirstAsync(cancellationToken);
        var invitation = await GetInvitationAsync(invitationId, cancellationToken);
        EnsureInvitationPending(invitation);
        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            invitation.Status = ProjectInvitationStatus.Expired;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new ApiException("Invitation has expired", HttpStatusCode.BadRequest);
        }

        var matchesInvitedUser = invitation.InvitedUserId == userId;
        var matchesEmail = NormalizeEmail(invitation.Email) == NormalizeEmail(userEmail);
        if (!matchesInvitedUser && !matchesEmail)
        {
            throw new ApiException("You cannot respond to this invitation", HttpStatusCode.Forbidden);
        }

        invitation.Status = status;
        invitation.InvitedUserId ??= userId;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        invitation.UpdatedAt = DateTimeOffset.UtcNow;
        if (status == ProjectInvitationStatus.Accepted)
        {
            await UpsertMemberAsync(invitation.ProjectId, userId, invitation.Role, userId, "Invitation accepted", cancellationToken);
            AddActivity(invitation.ProjectId, userId, ActivityType.MemberJoined, ActivityVisibility.MembersOnly, "Member joined", "A new member joined the project.", "ProjectInvitation", invitation.Id);
        }

        AddAudit(userId, $"ProjectInvitation.{status}", "ProjectInvitation", invitation.Id, status.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapInvitation(invitation);
    }

    private async Task UpsertMemberAsync(Guid projectId, Guid userId, ProjectMemberRole role, Guid actorUserId, string reason, CancellationToken cancellationToken)
    {
        var member = await dbContext.ProjectMembers.FirstOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == userId, cancellationToken);
        if (member is null)
        {
            member = new ProjectMember { ProjectId = projectId, UserId = userId, Role = role };
            dbContext.ProjectMembers.Add(member);
            AddHistory(projectId, member.Id, userId, actorUserId, "Added", null, role, reason);
        }
        else
        {
            var previous = member.Role;
            member.Role = role;
            member.IsActive = true;
            member.UpdatedAt = DateTimeOffset.UtcNow;
            AddHistory(projectId, member.Id, userId, actorUserId, "Reactivated", previous, role, reason);
        }
    }

    private IQueryable<ProjectMember> QueryMembers(Guid projectId) => dbContext.ProjectMembers
        .Include(member => member.User)
        .Where(member => member.ProjectId == projectId && member.IsActive);

    private async Task<ProjectMember> GetMemberAsync(Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        return await QueryMembers(projectId).FirstOrDefaultAsync(member => member.Id == memberId, cancellationToken)
            ?? throw new ApiException("Project member not found", HttpStatusCode.NotFound);
    }

    private async Task<ProjectInvitation> GetInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectInvitations.FirstOrDefaultAsync(item => item.Id == invitationId, cancellationToken)
            ?? throw new ApiException("Project invitation not found", HttpStatusCode.NotFound);
    }

    private async Task EnsureCanViewProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var canView = await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId && member.IsActive, cancellationToken) ||
            await dbContext.Projects.AnyAsync(project => project.Id == projectId && project.OwnerUserId == userId && !project.IsDeleted, cancellationToken);
        if (!canView) throw new ApiException("You do not have permission to view project team", HttpStatusCode.Forbidden);
    }

    private async Task EnsureCanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var canManage = await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId && member.IsActive && (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder), cancellationToken);
        if (!canManage) throw new ApiException("You do not have permission to manage project team", HttpStatusCode.Forbidden);
    }

    private async Task EnsureFounderAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var isFounder = await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == userId && member.IsActive && member.Role == ProjectMemberRole.Founder, cancellationToken);
        if (!isFounder) throw new ApiException("Only the founder can transfer ownership", HttpStatusCode.Forbidden);
    }

    private async Task RemoveAccessGrantIfAnyAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var grant = await dbContext.ProjectAccessGrants.FirstOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == userId, cancellationToken);
        if (grant is not null) dbContext.ProjectAccessGrants.Remove(grant);
    }

    private static void EnsureInvitationPending(ProjectInvitation invitation)
    {
        if (invitation.Status != ProjectInvitationStatus.Pending) throw new ApiException("Invitation is not pending", HttpStatusCode.BadRequest);
    }

    private void AddHistory(Guid projectId, Guid? memberId, Guid userId, Guid actorUserId, string action, ProjectMemberRole? fromRole, ProjectMemberRole? toRole, string? reason)
    {
        dbContext.ProjectMemberHistories.Add(new ProjectMemberHistory { ProjectId = projectId, MemberId = memberId, UserId = userId, ActorUserId = actorUserId, Action = action, FromRole = fromRole, ToRole = toRole, Reason = reason });
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string reason)
    {
        dbContext.AuditLogs.Add(new AuditLog { ActorUserId = actorUserId, Action = action, ResourceType = resourceType, ResourceId = resourceId, Reason = reason });
    }

    private void AddActivity(Guid projectId, Guid actorUserId, ActivityType type, ActivityVisibility visibility, string title, string? message, string targetType, Guid targetId)
    {
        dbContext.Activities.Add(new Activity
        {
            ProjectId = projectId,
            ActorUserId = actorUserId,
            Type = type,
            Visibility = visibility,
            Title = title,
            Message = message,
            TargetType = targetType,
            TargetId = targetId
        });
    }

    private static ProjectMemberDto MapMember(ProjectMember member) => new(member.Id, member.ProjectId, member.UserId, member.User.Email, member.User.FullName, member.Role, member.JoinedAt, member.IsActive);

    private static ProjectInvitationDto MapInvitation(ProjectInvitation invitation) => new(invitation.Id, invitation.ProjectId, invitation.InvitedByUserId, invitation.InvitedUserId, invitation.Email, invitation.Role, invitation.Message, invitation.Status, invitation.ExpiresAt, invitation.CreatedAt, invitation.RespondedAt);

    private static ProjectOwnershipTransferDto MapTransfer(ProjectOwnershipTransfer transfer, string? token = null) => new(transfer.Id, transfer.ProjectId, transfer.FromUserId, transfer.ToUserId, transfer.Status, transfer.ExpiresAt, transfer.AcceptedAt, token);

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException([new ErrorDetail("Required", "Reason is required", "reason")]);
    }

    private static void ValidateEmail(string email)
    {
        try { _ = new MailAddress(email); }
        catch { throw new ValidationException([new ErrorDetail("InvalidEmail", "Email is invalid", "email")]); }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value ?? principal.FindFirst("nameid")?.Value;
        if (!Guid.TryParse(userIdValue, out var userId)) throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        return userId;
    }
}

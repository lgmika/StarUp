using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.ProjectTeams.Dtos;

public sealed record ProjectMemberDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    string Email,
    string FullName,
    ProjectMemberRole Role,
    DateTimeOffset JoinedAt,
    bool IsActive);

public sealed record UpdateProjectMemberRequest(
    ProjectMemberRole Role,
    string Reason);

public sealed record CreateProjectInvitationRequest(
    string Email,
    ProjectMemberRole Role,
    string? Message = null);

public sealed record ProjectInvitationDto(
    Guid Id,
    Guid ProjectId,
    Guid InvitedByUserId,
    Guid? InvitedUserId,
    string Email,
    ProjectMemberRole Role,
    string? Message,
    ProjectInvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt);

public sealed record TransferOwnershipRequest(
    Guid ToUserId,
    string Reason);

public sealed record AcceptOwnershipTransferRequest(string Token);

public sealed record ProjectOwnershipTransferDto(
    Guid Id,
    Guid ProjectId,
    Guid FromUserId,
    Guid ToUserId,
    ProjectOwnershipTransferStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    string? ConfirmationToken = null);

public sealed record ProjectMemberHistoryDto(
    Guid Id,
    Guid ProjectId,
    Guid? MemberId,
    Guid UserId,
    Guid ActorUserId,
    string Action,
    ProjectMemberRole? FromRole,
    ProjectMemberRole? ToRole,
    string? Reason,
    DateTimeOffset CreatedAt);

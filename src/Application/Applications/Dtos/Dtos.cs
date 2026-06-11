using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Applications.Dtos;

public sealed record ApplyProjectRequest(
    Guid? CvId,
    string CoverLetter);

public sealed record ApplicationDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid ApplicantUserId,
    string ApplicantEmail,
    string ApplicantFullName,
    Guid? CvId,
    string? CvTitle,
    string CoverLetter,
    ApplicationStatus Status,
    string? FounderNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ApplicationStatusHistoryDto(
    Guid Id,
    ApplicationStatus FromStatus,
    ApplicationStatus ToStatus,
    Guid ChangedByUserId,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record ApplicationDetailDto(
    ApplicationDto Application,
    IReadOnlyCollection<ApplicationStatusHistoryDto> StatusHistory);

public sealed record ApplicationDecisionRequest(
    string? Reason,
    string? FounderNote);


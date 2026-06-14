using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Interviews.Dtos;

public sealed record InterviewParticipantDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FullName,
    bool IsRequired);

public sealed record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    Guid ProjectId,
    Guid ScheduledByUserId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string TimeZone,
    InterviewMeetingType MeetingType,
    string? MeetingUrl,
    string? Location,
    string? Note,
    InterviewStatus Status,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<InterviewParticipantDto> Participants);

public sealed record InterviewStatusHistoryDto(
    Guid Id,
    InterviewStatus FromStatus,
    InterviewStatus ToStatus,
    Guid ChangedByUserId,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record InterviewDetailDto(
    InterviewDto Interview,
    IReadOnlyCollection<InterviewStatusHistoryDto> History);

public sealed record CreateInterviewRequest(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string TimeZone,
    InterviewMeetingType MeetingType,
    string? MeetingUrl = null,
    string? Location = null,
    string? Note = null,
    IReadOnlyCollection<Guid>? ParticipantUserIds = null);

public sealed record UpdateInterviewRequest(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string TimeZone,
    InterviewMeetingType MeetingType,
    string? MeetingUrl = null,
    string? Location = null,
    string? Note = null,
    IReadOnlyCollection<Guid>? ParticipantUserIds = null);

public sealed record InterviewDecisionRequest(string Reason);

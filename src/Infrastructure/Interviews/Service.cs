using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Email.Models;
using StartupConnect.Application.Interviews.Dtos;
using StartupConnect.Application.Interviews.Interfaces;
using StartupConnect.Application.Notifications.Dtos;
using StartupConnect.Application.Notifications.Interfaces;
using StartupConnect.Application.Realtime.Interfaces;
using StartupConnect.Domain.Entities;
using StartupConnect.Domain.Enums;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Infrastructure.Email;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Interviews;

public sealed class InterviewService(
    AppDbContext dbContext,
    INotificationService notificationService,
    EmailOutboxDispatcher emailOutboxDispatcher,
    IRealtimeNotifier realtimeNotifier) : IInterviewService
{
    public async Task<InterviewDto> CreateAsync(ClaimsPrincipal principal, Guid applicationId, CreateInterviewRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateSchedule(request.StartAt, request.EndAt, request.TimeZone, request.MeetingType, request.MeetingUrl, request.Location, request.Note, allowPast: false);
        var application = await GetApplicationAsync(applicationId, cancellationToken);
        await EnsureCanManageProjectAsync(application.ProjectId, actorUserId, cancellationToken);

        var participantIds = await BuildParticipantIdsAsync(application, actorUserId, request.ParticipantUserIds, cancellationToken);
        await EnsureNoScheduleConflictAsync(participantIds, request.StartAt, request.EndAt, null, cancellationToken);

        var interview = new ProjectInterview
        {
            ApplicationId = application.Id,
            ProjectId = application.ProjectId,
            ScheduledByUserId = actorUserId,
            StartAt = request.StartAt.ToUniversalTime(),
            EndAt = request.EndAt.ToUniversalTime(),
            TimeZone = request.TimeZone.Trim(),
            MeetingType = request.MeetingType,
            MeetingUrl = string.IsNullOrWhiteSpace(request.MeetingUrl) ? null : request.MeetingUrl.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = InterviewStatus.Scheduled
        };

        dbContext.ProjectInterviews.Add(interview);
        foreach (var participantId in participantIds)
        {
            dbContext.InterviewParticipants.Add(new InterviewParticipant { Interview = interview, UserId = participantId });
        }

        AddHistory(interview, InterviewStatus.Scheduled, InterviewStatus.Scheduled, actorUserId, "Interview scheduled");
        AddAudit(actorUserId, "Interview.Create", "ProjectInterview", interview.Id, "Interview scheduled");
        if (application.Status is ApplicationStatus.Pending or ApplicationStatus.Shortlisted)
        {
            AddApplicationHistory(application, application.Status, ApplicationStatus.Interviewing, actorUserId, "Interview scheduled");
            application.Status = ApplicationStatus.Interviewing;
            application.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyParticipantsAsync(interview.Id, "Interview scheduled", "A project interview has been scheduled.", cancellationToken);
        return await NotifyAndReturnInterviewAsync(interview.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<InterviewDto>> GetApplicationInterviewsAsync(ClaimsPrincipal principal, Guid applicationId, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        var application = await GetApplicationAsync(applicationId, cancellationToken);
        var canView = application.ApplicantUserId == actorUserId || await CanManageProjectAsync(application.ProjectId, actorUserId, cancellationToken);
        if (!canView)
        {
            throw new ApiException("You do not have permission to view interviews for this application", HttpStatusCode.Forbidden);
        }

        return await MapInterviewsAsync(dbContext.ProjectInterviews.Where(interview => interview.ApplicationId == applicationId), cancellationToken);
    }

    public async Task<IReadOnlyCollection<InterviewDto>> GetMyInterviewsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var query = dbContext.ProjectInterviews
            .Where(interview => dbContext.InterviewParticipants.Any(participant => participant.InterviewId == interview.Id && participant.UserId == userId));

        return await MapInterviewsAsync(query, cancellationToken);
    }

    public async Task<InterviewDto> UpdateAsync(ClaimsPrincipal principal, Guid interviewId, UpdateInterviewRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateSchedule(request.StartAt, request.EndAt, request.TimeZone, request.MeetingType, request.MeetingUrl, request.Location, request.Note, allowPast: false);
        var interview = await GetInterviewAsync(interviewId, cancellationToken);
        await EnsureCanManageProjectAsync(interview.ProjectId, actorUserId, cancellationToken);
        EnsureMutable(interview);

        var participantIds = await BuildParticipantIdsAsync(interview.Application, actorUserId, request.ParticipantUserIds, cancellationToken);
        await EnsureNoScheduleConflictAsync(participantIds, request.StartAt, request.EndAt, interviewId, cancellationToken);

        var previousStatus = interview.Status;
        interview.StartAt = request.StartAt.ToUniversalTime();
        interview.EndAt = request.EndAt.ToUniversalTime();
        interview.TimeZone = request.TimeZone.Trim();
        interview.MeetingType = request.MeetingType;
        interview.MeetingUrl = string.IsNullOrWhiteSpace(request.MeetingUrl) ? null : request.MeetingUrl.Trim();
        interview.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        interview.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        interview.Status = InterviewStatus.Rescheduled;
        interview.UpdatedAt = DateTimeOffset.UtcNow;

        var existingParticipants = await dbContext.InterviewParticipants.Where(participant => participant.InterviewId == interviewId).ToArrayAsync(cancellationToken);
        dbContext.InterviewParticipants.RemoveRange(existingParticipants);
        foreach (var participantId in participantIds)
        {
            dbContext.InterviewParticipants.Add(new InterviewParticipant { InterviewId = interviewId, UserId = participantId });
        }

        AddHistory(interview, previousStatus, InterviewStatus.Rescheduled, actorUserId, "Interview rescheduled");
        AddAudit(actorUserId, "Interview.Update", "ProjectInterview", interview.Id, "Interview rescheduled");
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyParticipantsAsync(interview.Id, "Interview rescheduled", "A project interview has been rescheduled.", cancellationToken);
        return await NotifyAndReturnInterviewAsync(interview.Id, cancellationToken);
    }

    public async Task<InterviewDto> CancelAsync(ClaimsPrincipal principal, Guid interviewId, InterviewDecisionRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        var interview = await GetInterviewAsync(interviewId, cancellationToken);
        await EnsureCanManageProjectAsync(interview.ProjectId, actorUserId, cancellationToken);
        EnsureMutable(interview);

        var previous = interview.Status;
        interview.Status = InterviewStatus.Cancelled;
        interview.CancellationReason = request.Reason.Trim();
        interview.UpdatedAt = DateTimeOffset.UtcNow;
        AddHistory(interview, previous, InterviewStatus.Cancelled, actorUserId, request.Reason);
        AddAudit(actorUserId, "Interview.Cancel", "ProjectInterview", interview.Id, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyParticipantsAsync(interview.Id, "Interview cancelled", "A project interview has been cancelled.", cancellationToken);
        return await NotifyAndReturnInterviewAsync(interview.Id, cancellationToken);
    }

    public async Task<InterviewDto> CompleteAsync(ClaimsPrincipal principal, Guid interviewId, InterviewDecisionRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId(principal);
        ValidateReason(request.Reason);
        var interview = await GetInterviewAsync(interviewId, cancellationToken);
        await EnsureCanManageProjectAsync(interview.ProjectId, actorUserId, cancellationToken);
        if (interview.Status is InterviewStatus.Cancelled or InterviewStatus.Completed)
        {
            throw new ApiException("Interview cannot be completed from its current status", HttpStatusCode.BadRequest);
        }

        var previous = interview.Status;
        interview.Status = InterviewStatus.Completed;
        interview.UpdatedAt = DateTimeOffset.UtcNow;
        AddHistory(interview, previous, InterviewStatus.Completed, actorUserId, request.Reason);
        AddAudit(actorUserId, "Interview.Complete", "ProjectInterview", interview.Id, request.Reason);
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyParticipantsAsync(interview.Id, "Interview completed", "A project interview has been marked as completed.", cancellationToken);
        return await NotifyAndReturnInterviewAsync(interview.Id, cancellationToken);
    }

    private async Task<ProjectApplication> GetApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectApplications
            .Include(application => application.Project)
            .Include(application => application.ApplicantUser)
            .FirstOrDefaultAsync(application => application.Id == applicationId, cancellationToken)
            ?? throw new ApiException("Application not found", HttpStatusCode.NotFound);
    }

    private async Task<ProjectInterview> GetInterviewAsync(Guid interviewId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectInterviews
            .Include(interview => interview.Application)
            .ThenInclude(application => application.ApplicantUser)
            .FirstOrDefaultAsync(interview => interview.Id == interviewId, cancellationToken)
            ?? throw new ApiException("Interview not found", HttpStatusCode.NotFound);
    }

    private async Task<IReadOnlyCollection<Guid>> BuildParticipantIdsAsync(ProjectApplication application, Guid schedulerId, IReadOnlyCollection<Guid>? requestedIds, CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid> { application.ApplicantUserId, schedulerId };
        if (requestedIds is not null)
        {
            foreach (var id in requestedIds.Where(id => id != Guid.Empty))
            {
                ids.Add(id);
            }
        }

        var validProjectMembers = await dbContext.ProjectMembers
            .Where(member => member.ProjectId == application.ProjectId && member.IsActive)
            .Select(member => member.UserId)
            .ToArrayAsync(cancellationToken);
        foreach (var id in ids)
        {
            if (id != application.ApplicantUserId && !validProjectMembers.Contains(id))
            {
                throw new ApiException("Interview participants must be the applicant or active project members", HttpStatusCode.BadRequest);
            }
        }

        return ids.ToArray();
    }

    private async Task EnsureNoScheduleConflictAsync(IReadOnlyCollection<Guid> participantIds, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludedInterviewId, CancellationToken cancellationToken)
    {
        var start = startAt.ToUniversalTime();
        var end = endAt.ToUniversalTime();
        var hasConflict = await dbContext.ProjectInterviews.AnyAsync(interview =>
            (!excludedInterviewId.HasValue || interview.Id != excludedInterviewId.Value) &&
            interview.Status != InterviewStatus.Cancelled &&
            interview.Status != InterviewStatus.Completed &&
            interview.StartAt < end &&
            interview.EndAt > start &&
            dbContext.InterviewParticipants.Any(participant => participant.InterviewId == interview.Id && participantIds.Contains(participant.UserId)),
            cancellationToken);

        if (hasConflict)
        {
            throw new ApiException("Interview schedule conflicts with an existing interview", HttpStatusCode.Conflict);
        }
    }

    private async Task NotifyParticipantsAsync(Guid interviewId, string title, string message, CancellationToken cancellationToken)
    {
        var participants = await dbContext.InterviewParticipants
            .Include(participant => participant.User)
            .Where(participant => participant.InterviewId == interviewId)
            .ToArrayAsync(cancellationToken);

        foreach (var participant in participants)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                participant.UserId,
                NotificationType.System,
                title,
                message,
                "ProjectInterview",
                interviewId,
                $"/interviews/{interviewId}"), cancellationToken);

            emailOutboxDispatcher.QueueNotification(
                participant.UserId,
                participant.User.Email,
                new NotificationEmailModel(title, title, message, $"/interviews/{interviewId}", "View interview"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<InterviewDto>> MapInterviewsAsync(IQueryable<ProjectInterview> query, CancellationToken cancellationToken)
    {
        var interviews = await query.OrderByDescending(interview => interview.StartAt).ToArrayAsync(cancellationToken);
        var result = new List<InterviewDto>();
        foreach (var interview in interviews)
        {
            result.Add(await GetInterviewDtoAsync(interview.Id, cancellationToken));
        }

        return result;
    }

    private async Task<InterviewDto> NotifyAndReturnInterviewAsync(Guid interviewId, CancellationToken cancellationToken)
    {
        var interview = await GetInterviewDtoAsync(interviewId, cancellationToken);
        var participantIds = interview.Participants.Select(participant => participant.UserId).ToArray();
        await realtimeNotifier.InterviewChangedAsync(interview.ProjectId, participantIds, interview, cancellationToken);
        return interview;
    }

    private async Task<InterviewDto> GetInterviewDtoAsync(Guid interviewId, CancellationToken cancellationToken)
    {
        var interview = await dbContext.ProjectInterviews.FirstAsync(item => item.Id == interviewId, cancellationToken);
        var participants = await dbContext.InterviewParticipants
            .Include(participant => participant.User)
            .Where(participant => participant.InterviewId == interviewId)
            .OrderBy(participant => participant.User.Email)
            .Select(participant => new InterviewParticipantDto(participant.Id, participant.UserId, participant.User.Email, participant.User.FullName, participant.IsRequired))
            .ToArrayAsync(cancellationToken);

        return new InterviewDto(
            interview.Id,
            interview.ApplicationId,
            interview.ProjectId,
            interview.ScheduledByUserId,
            interview.StartAt,
            interview.EndAt,
            interview.TimeZone,
            interview.MeetingType,
            interview.MeetingUrl,
            interview.Location,
            interview.Note,
            interview.Status,
            interview.CancellationReason,
            interview.CreatedAt,
            participants);
    }

    private void AddHistory(ProjectInterview interview, InterviewStatus fromStatus, InterviewStatus toStatus, Guid changedByUserId, string? reason)
    {
        dbContext.InterviewStatusHistories.Add(new InterviewStatusHistory
        {
            Interview = interview,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        });
    }

    private void AddApplicationHistory(ProjectApplication application, ApplicationStatus fromStatus, ApplicationStatus toStatus, Guid changedByUserId, string? reason)
    {
        dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Application = application,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        });
    }

    private void AddAudit(Guid actorUserId, string action, string resourceType, Guid resourceId, string? reason)
    {
        dbContext.AuditLogs.Add(new AuditLog { ActorUserId = actorUserId, Action = action, ResourceType = resourceType, ResourceId = resourceId, Reason = reason });
    }

    private async Task EnsureCanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        if (!await CanManageProjectAsync(projectId, userId, cancellationToken))
        {
            throw new ApiException("Only project founders or co-founders can manage interviews", HttpStatusCode.Forbidden);
        }
    }

    private async Task<bool> CanManageProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers.AnyAsync(member =>
            member.ProjectId == projectId &&
            member.UserId == userId &&
            member.IsActive &&
            (member.Role == ProjectMemberRole.Founder || member.Role == ProjectMemberRole.CoFounder),
            cancellationToken);
    }

    private static void ValidateSchedule(DateTimeOffset startAt, DateTimeOffset endAt, string timeZone, InterviewMeetingType meetingType, string? meetingUrl, string? location, string? note, bool allowPast)
    {
        if (startAt.ToUniversalTime() >= endAt.ToUniversalTime())
        {
            throw new ValidationException([new ErrorDetail("InvalidTimeRange", "End time must be after start time", "endAt")]);
        }

        if (!allowPast && startAt.ToUniversalTime() <= DateTimeOffset.UtcNow)
        {
            throw new ValidationException([new ErrorDetail("InvalidStartAt", "Interview cannot be scheduled in the past", "startAt")]);
        }

        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new ValidationException([new ErrorDetail("Required", "Time zone is required", "timeZone")]);
        }

        ValidateMaximumLength(timeZone, 120, "timeZone");
        ValidateMaximumLength(meetingUrl, 1000, "meetingUrl");
        ValidateMaximumLength(location, 500, "location");
        ValidateMaximumLength(note, 2000, "note");

        if (!string.IsNullOrWhiteSpace(meetingUrl) &&
            (!Uri.TryCreate(meetingUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
        {
            throw new ValidationException([new ErrorDetail("InvalidUrl", "Meeting URL must be an absolute HTTP/HTTPS URL", "meetingUrl")]);
        }

        if (meetingType == InterviewMeetingType.Online && string.IsNullOrWhiteSpace(meetingUrl))
        {
            throw new ValidationException([new ErrorDetail("Required", "Meeting URL is required for online interviews", "meetingUrl")]);
        }

        if (meetingType == InterviewMeetingType.InPerson && string.IsNullOrWhiteSpace(location))
        {
            throw new ValidationException([new ErrorDetail("Required", "Location is required for in-person interviews", "location")]);
        }
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException([new ErrorDetail("Required", "Reason is required", "reason")]);
        }

        ValidateMaximumLength(reason, 1000, "reason");
    }

    private static void ValidateMaximumLength(string? value, int maximum, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maximum)
        {
            throw new ValidationException([new ErrorDetail("TooLong", $"{field} must be at most {maximum} characters", field)]);
        }
    }

    private static void EnsureMutable(ProjectInterview interview)
    {
        if (interview.Status is InterviewStatus.Cancelled or InterviewStatus.Completed)
        {
            throw new ApiException("Interview cannot be changed from its current status", HttpStatusCode.BadRequest);
        }
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value ?? principal.FindFirst("nameid")?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}

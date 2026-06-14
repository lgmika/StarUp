using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Dashboards.Dtos;

public sealed record DashboardQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int TimezoneOffsetMinutes = 0);

public sealed record CountByStatusDto(string Status, int Count);

public sealed record UserDashboardDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TimezoneOffsetMinutes,
    int Applications,
    IReadOnlyCollection<CountByStatusDto> ApplicationsByStatus,
    int UpcomingInterviews,
    int JoinedProjects,
    int SavedProjects,
    int ProfileCompletionPercent);

public sealed record FounderProjectDashboardDto(
    Guid ProjectId,
    string ProjectTitle,
    DateTimeOffset From,
    DateTimeOffset To,
    int TimezoneOffsetMinutes,
    int ProjectViews,
    int SavedCount,
    int Applications,
    double ApplicationConversionRate,
    int TeamSize,
    int InvestorInterests,
    int NdaAgreements,
    IReadOnlyCollection<CountByStatusDto> ApplicationsByStatus,
    IReadOnlyCollection<CountByStatusDto> InvestorInterestsByStatus,
    IReadOnlyCollection<ProjectStatusHistoryDto> ProjectStatusHistory);

public sealed record ProjectStatusHistoryDto(
    Guid Id,
    ApplicationStatus FromStatus,
    ApplicationStatus ToStatus,
    Guid ChangedByUserId,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record InvestorDashboardDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TimezoneOffsetMinutes,
    int InterestedProjects,
    IReadOnlyCollection<CountByStatusDto> InterestStatus,
    int NdaPending,
    int AcceptedAccess,
    int SavedProjects);

using System.Security.Claims;
using StartupConnect.Application.Activities.Dtos;

namespace StartupConnect.Application.Activities.Interfaces;

public interface IActivityService
{
    Task<ActivityListResponse> GetFeedAsync(ClaimsPrincipal? principal, ActivityQuery query, CancellationToken cancellationToken);

    Task<ActivityListResponse> GetProjectActivitiesAsync(ClaimsPrincipal? principal, Guid projectId, ActivityQuery query, CancellationToken cancellationToken);
}

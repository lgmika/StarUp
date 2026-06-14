using System.Security.Claims;
using StartupConnect.Application.Interviews.Dtos;

namespace StartupConnect.Application.Interviews.Interfaces;

public interface IInterviewService
{
    Task<InterviewDto> CreateAsync(ClaimsPrincipal principal, Guid applicationId, CreateInterviewRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InterviewDto>> GetApplicationInterviewsAsync(ClaimsPrincipal principal, Guid applicationId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InterviewDto>> GetMyInterviewsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<InterviewDto> UpdateAsync(ClaimsPrincipal principal, Guid interviewId, UpdateInterviewRequest request, CancellationToken cancellationToken);

    Task<InterviewDto> CancelAsync(ClaimsPrincipal principal, Guid interviewId, InterviewDecisionRequest request, CancellationToken cancellationToken);

    Task<InterviewDto> CompleteAsync(ClaimsPrincipal principal, Guid interviewId, InterviewDecisionRequest request, CancellationToken cancellationToken);
}

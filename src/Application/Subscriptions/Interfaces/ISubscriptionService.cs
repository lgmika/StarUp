using System.Security.Claims;
using StartupConnect.Application.Subscriptions.Dtos;

namespace StartupConnect.Application.Subscriptions.Interfaces;

public interface ISubscriptionService
{
    Task<IReadOnlyCollection<SubscriptionPlanDto>> GetPlansAsync(CancellationToken cancellationToken);

    Task<SubscriptionDto> GetMySubscriptionAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<CheckoutResponse> CreateCheckoutAsync(ClaimsPrincipal principal, CheckoutRequest request, CancellationToken cancellationToken);

    Task<SubscriptionDto> CancelAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<SubscriptionDto> ResumeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<PaymentWebhookResult> HandleWebhookAsync(string payloadJson, string? signature, CancellationToken cancellationToken);
}

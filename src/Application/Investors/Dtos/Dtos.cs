using StartupConnect.Application.Projects.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Investors.Dtos;

public sealed record InvestorProfileDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? OrganizationName,
    string? Bio,
    string? InvestmentFocus,
    string? WebsiteUrl,
    string? LinkedInUrl,
    decimal? MinTicketSize,
    decimal? MaxTicketSize);

public sealed record UpsertInvestorProfileRequest(
    string DisplayName,
    string? OrganizationName,
    string? Bio,
    string? InvestmentFocus,
    string? WebsiteUrl,
    string? LinkedInUrl,
    decimal? MinTicketSize,
    decimal? MaxTicketSize);

public sealed record CreateInvestorInterestRequest(string Message);

public sealed record InvestorInterestDecisionRequest(string? Response);

public sealed record InvestorInterestDto(
    Guid Id,
    Guid ProjectId,
    string ProjectTitle,
    Guid InvestorUserId,
    string InvestorEmail,
    string Message,
    InvestorInterestStatus Status,
    string? FounderResponse,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record InvestorProjectDiscoveryDto(
    ProjectSummaryDto Project,
    string? InvestorSummary);


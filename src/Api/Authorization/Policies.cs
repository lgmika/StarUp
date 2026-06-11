namespace StartupConnect.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string VerifiedUserOnly = "VerifiedUserOnly";
    public const string BusinessOnly = "BusinessOnly";
    public const string InvestorOnly = "InvestorOnly";
    public const string ModeratorOnly = "ModeratorOnly";
    public const string AdminOnly = "AdminOnly";
    public const string ModeratorOrAdmin = "ModeratorOrAdmin";
}


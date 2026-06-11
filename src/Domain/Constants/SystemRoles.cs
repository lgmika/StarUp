namespace StartupConnect.Domain.Constants;

public static class SystemRoles
{
    public const string Guest = "Guest";
    public const string User = "User";
    public const string VerifiedUser = "VerifiedUser";
    public const string Business = "Business";
    public const string Investor = "Investor";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";

    public static readonly string[] All =
    [
        Guest,
        User,
        VerifiedUser,
        Business,
        Investor,
        Moderator,
        Admin
    ];
}


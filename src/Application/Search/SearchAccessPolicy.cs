using StartupConnect.Domain.Enums;

namespace StartupConnect.Application.Search;

public static class SearchAccessPolicy
{
    public static bool CanSeeProject(
        ProjectStatus status,
        ProjectVisibility visibility,
        bool isOwner,
        bool isMember,
        bool hasAccessGrant)
    {
        if (status == ProjectStatus.Published && visibility is ProjectVisibility.Public or ProjectVisibility.Limited)
        {
            return true;
        }

        return isOwner || isMember || hasAccessGrant;
    }

    public static bool CanSearchMemberProfile(ContactVisibility visibility)
    {
        return visibility == ContactVisibility.Public;
    }
}

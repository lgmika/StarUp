using StartupConnect.Application.Profiles.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ProfileDtoTests
{
    [Fact]
    public void ProfileDto_Should_Support_Contact_Visibility()
    {
        var profile = new ProfileDto(
            Guid.NewGuid(),
            "user@example.com",
            "Test User",
            "Founder",
            "Building StartupConnect",
            "Vietnam",
            null,
            null,
            null,
            null,
            ContactVisibility.Public,
            [],
            []);

        Assert.Equal(ContactVisibility.Public, profile.ContactVisibility);
    }
}


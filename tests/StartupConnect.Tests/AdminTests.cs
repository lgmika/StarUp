using StartupConnect.Application.Admin.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class AdminDtoTests
{
    [Fact]
    public void AdminUserDto_Should_Represent_User_Status_And_Roles()
    {
        var user = new AdminUserDto(
            Guid.NewGuid(),
            "admin@example.com",
            "Admin User",
            true,
            UserStatus.Suspended,
            true,
            DateTimeOffset.UtcNow.AddDays(1),
            "Policy violation",
            null,
            null,
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ["Admin"]);

        Assert.Equal(UserStatus.Suspended, user.Status);
        Assert.True(user.IsSuspended);
        Assert.Contains("Admin", user.Roles);
    }
}

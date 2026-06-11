using StartupConnect.Infrastructure.Auth;

namespace StartupConnect.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_Should_Return_True_For_Valid_Password()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("StrongPassword123");

        Assert.True(hasher.Verify("StrongPassword123", hash));
        Assert.False(hasher.Verify("WrongPassword123", hash));
    }
}


using StartupConnect.Infrastructure.Auth;
using StartupConnect.Domain.Entities;
using StartupConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    [Theory]
    [InlineData("")]
    [InlineData("not-a-password-hash")]
    [InlineData("PBKDF2-SHA256.100000.invalid-base64.invalid-base64")]
    [InlineData("PBKDF2-SHA256.1.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("PBKDF2-SHA256.999999999.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void Verify_Should_Reject_Malformed_Hashes_Without_Throwing(string hash)
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("StrongPassword123", hash));
    }

    [Fact]
    public void RefreshToken_Revocation_Should_Be_A_Concurrency_Token()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=startupconnect_model_test")
            .Options;
        using var dbContext = new AppDbContext(options);

        var property = dbContext.Model.FindEntityType(typeof(RefreshToken))!
            .FindProperty(nameof(RefreshToken.RevokedAt));

        Assert.NotNull(property);
        Assert.True(property!.IsConcurrencyToken);
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StartupConnect.Infrastructure.Email;

namespace StartupConnect.Tests;

public sealed class EmailTests
{
    [Fact]
    public async Task DevelopmentEmailService_Should_Write_Verification_Email_To_File()
    {
        var directory = Path.Combine(Path.GetTempPath(), "startupconnect-email-tests", Guid.NewGuid().ToString("N"));
        var service = new DevelopmentEmailService(
            Options.Create(new EmailOptions
            {
                DevLogDirectory = directory,
                FromEmail = "no-reply@test.local",
                FromName = "StartupConnect Test"
            }),
            NullLogger<DevelopmentEmailService>.Instance);

        await service.SendEmailVerificationAsync(
            "founder@example.com",
            "http://localhost:3000/auth/verify-email?token=abc",
            CancellationToken.None);

        var file = Assert.Single(Directory.GetFiles(directory, "*.eml"));
        var content = await File.ReadAllTextAsync(file);

        Assert.Contains("To: founder@example.com", content);
        Assert.Contains("TEXT:", content);
        Assert.Contains("HTML:", content);
        Assert.Contains("http://localhost:3000/auth/verify-email?token=abc", content);
    }
}

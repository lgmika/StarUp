using StartupConnect.Application.Investors.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class InvestorDtoTests
{
    [Fact]
    public void InvestorInterestDto_Should_Represent_Status()
    {
        var interest = new InvestorInterestDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Project",
            Guid.NewGuid(),
            "investor@example.com",
            "Interested in learning more.",
            InvestorInterestStatus.Pending,
            null,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal(InvestorInterestStatus.Pending, interest.Status);
    }
}


using StartupConnect.Application.Reports.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class ReportDtoTests
{
    [Fact]
    public void ReportDto_Should_Represent_Status_And_Reason()
    {
        var report = new ReportDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "reporter@example.com",
            "Project",
            Guid.NewGuid(),
            ReportReasonCode.Scam,
            "Suspicious funding request.",
            "https://example.com/evidence",
            ReportStatus.Pending,
            null,
            null,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal(ReportReasonCode.Scam, report.ReasonCode);
        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.Equal("Project", report.TargetType);
    }
}

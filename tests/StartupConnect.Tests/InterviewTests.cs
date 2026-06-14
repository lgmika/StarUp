using StartupConnect.Application.Interviews.Dtos;
using StartupConnect.Domain.Enums;

namespace StartupConnect.Tests;

public sealed class InterviewDtoTests
{
    [Fact]
    public void InterviewDto_Should_Represent_Schedule_And_Status()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var interview = new InterviewDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            start.AddMinutes(45),
            "Asia/Saigon",
            InterviewMeetingType.Online,
            "https://meet.example.com/session",
            null,
            "Technical discussion",
            InterviewStatus.Scheduled,
            null,
            DateTimeOffset.UtcNow,
            []);

        Assert.Equal(InterviewStatus.Scheduled, interview.Status);
        Assert.Equal(InterviewMeetingType.Online, interview.MeetingType);
        Assert.True(interview.EndAt > interview.StartAt);
    }
}

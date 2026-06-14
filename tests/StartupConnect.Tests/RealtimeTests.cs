using StartupConnect.Application.Realtime;

namespace StartupConnect.Tests;

public sealed class RealtimeTests
{
    [Fact]
    public void RealtimeEventNames_Should_Expose_Stable_Client_Event_Names()
    {
        Assert.Equal("notification.created", RealtimeEventNames.NotificationCreated);
        Assert.Equal("notification.read", RealtimeEventNames.NotificationRead);
        Assert.Equal("notifications.readAll", RealtimeEventNames.NotificationsReadAll);
        Assert.Equal("message.created", RealtimeEventNames.MessageCreated);
        Assert.Equal("message.read", RealtimeEventNames.MessageRead);
        Assert.Equal("application.statusChanged", RealtimeEventNames.ApplicationStatusChanged);
        Assert.Equal("interview.changed", RealtimeEventNames.InterviewChanged);
        Assert.Equal("project.status.changed", RealtimeEventNames.ProjectStatusChanged);
        Assert.Equal("investorInterest.changed", RealtimeEventNames.InvestorInterestChanged);
        Assert.Equal("report.changed", RealtimeEventNames.ReportChanged);
        Assert.Equal("conversation.created", RealtimeEventNames.ConversationCreated);
        Assert.Equal("billing.subscription.changed", RealtimeEventNames.BillingSubscriptionChanged);
        Assert.Equal("nda.agreement.accepted", RealtimeEventNames.NdaAgreementAccepted);
    }
}

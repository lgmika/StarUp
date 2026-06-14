namespace StartupConnect.Application.Realtime;

public static class RealtimeEventNames
{
    public const string NotificationCreated = "notification.created";
    public const string NotificationRead = "notification.read";
    public const string NotificationDeleted = "notification.deleted";
    public const string NotificationsReadAll = "notifications.readAll";
    public const string MessageCreated = "message.created";
    public const string MessageDeleted = "message.deleted";
    public const string MessageRead = "message.read";
    public const string ConversationCreated = "conversation.created";
    public const string ProjectStatusChanged = "project.status.changed";
    public const string ApplicationStatusChanged = "application.statusChanged";
    public const string ApplicationStatusChangedV2 = "application.status.changed";
    public const string InvestorInterestChanged = "investorInterest.changed";
    public const string InterviewChanged = "interview.changed";
    public const string ReportChanged = "report.changed";
    public const string NdaAgreementAccepted = "nda.agreement.accepted";
    public const string BillingSubscriptionChanged = "billing.subscription.changed";
}

namespace StartupConnect.Api.Realtime;

public static class RealtimeGroups
{
    public static string User(Guid userId) => $"user:{userId}";

    public static string Project(Guid projectId) => $"project:{projectId}";

    public static string Conversation(Guid conversationId) => $"conversation:{conversationId}";
}

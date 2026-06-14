namespace StartupConnect.Application.Dashboards;

public static class DashboardMetrics
{
    public static int CompletionPercent(int completedFields, int totalFields)
    {
        if (totalFields <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round((double)completedFields / totalFields * 100), 0, 100);
    }

    public static double ConversionRate(int converted, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return Math.Round((double)converted / total, 4);
    }
}

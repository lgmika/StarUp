namespace StartupConnect.Infrastructure;

public static class Pagination
{
    public static int GetOffset(int page, int pageSize)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Max(pageSize, 1);
        return (int)Math.Min((long)(normalizedPage - 1) * normalizedPageSize, int.MaxValue);
    }
}

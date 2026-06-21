using StartupConnect.Infrastructure;

namespace StartupConnect.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(2, 20, 20)]
    [InlineData(0, 20, 0)]
    [InlineData(int.MaxValue, 100, int.MaxValue)]
    public void GetOffset_Should_Normalize_And_Prevent_Integer_Overflow(int page, int pageSize, int expected)
    {
        Assert.Equal(expected, Pagination.GetOffset(page, pageSize));
    }
}

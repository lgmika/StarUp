using StartupConnect.Shared.Responses;

namespace StartupConnect.Tests;

public sealed class ApiResponseTests
{
    [Fact]
    public void Ok_Should_Return_Standard_Success_Response()
    {
        var response = ApiResponse<object>.Ok(new { id = 1 });

        Assert.True(response.Success);
        Assert.Equal("Request completed successfully", response.Message);
        Assert.NotNull(response.Data);
    }
}


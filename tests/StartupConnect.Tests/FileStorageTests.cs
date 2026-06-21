using Microsoft.Extensions.Options;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Infrastructure.Files;

namespace StartupConnect.Tests;

public sealed class FileStorageTests
{
    [Fact]
    public async Task LocalFileStorageService_Should_Create_And_Validate_Signed_Url()
    {
        var service = new LocalFileStorageService(Options.Create(new FileStorageOptions
        {
            PublicBaseUrl = "http://localhost:8080",
            LocalRootPath = Path.Combine(Path.GetTempPath(), "startupconnect-file-tests"),
            SigningKey = "test-signing-key"
        }));

        var url = await service.CreateDownloadUrlAsync("cvs/2026/06/file.pdf", TimeSpan.FromMinutes(5), CancellationToken.None);
        var uri = new Uri(url);
        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1]));

        Assert.True(service.ValidateDownloadUrl(
            query["path"],
            long.Parse(query["expires"]),
            query["signature"]));
    }

    [Fact]
    public void StoredFileResult_Should_Not_Expose_Absolute_FileSystem_Path()
    {
        var result = new StoredFileResult(
            "cv.pdf",
            "generated.pdf",
            "cvs/2026/06/generated.pdf",
            "application/pdf",
            128);

        Assert.DoesNotContain(":\\", result.StoragePath);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public async Task LocalFileStorageService_Should_Reject_Sibling_Path_Traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "startupconnect-storage-root");
        var service = new LocalFileStorageService(Options.Create(new FileStorageOptions
        {
            LocalRootPath = root,
            SigningKey = "test-signing-key"
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OpenReadAsync("../startupconnect-storage-root-evil/file.pdf", CancellationToken.None));
    }

    [Fact]
    public void LocalFileStorageService_Should_Reject_Invalid_Expiry()
    {
        var service = new LocalFileStorageService(Options.Create(new FileStorageOptions
        {
            SigningKey = "test-signing-key"
        }));

        Assert.False(service.ValidateDownloadUrl("file.pdf", long.MaxValue, "invalid"));
    }
}

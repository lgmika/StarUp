using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Files.Interfaces;

namespace StartupConnect.Infrastructure.Files;

public sealed class LocalFileStorageService(IOptions<FileStorageOptions> optionsAccessor) : IFileStorageService
{
    private readonly FileStorageOptions options = optionsAccessor.Value;

    public async Task<StoredFileResult> UploadAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string category,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var safeCategory = SanitizeSegment(category);
        var relativePath = $"{safeCategory}/{DateTimeOffset.UtcNow:yyyy/MM}/{storedFileName}".Replace('\\', '/');
        var absolutePath = ResolvePath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await using var output = File.Create(absolutePath);
        await content.CopyToAsync(output, cancellationToken);

        return new StoredFileResult(
            Path.GetFileName(originalFileName),
            storedFileName,
            relativePath,
            contentType,
            sizeInBytes);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var absolutePath = ResolvePath(storagePath);
        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var absolutePath = ResolvePath(storagePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public Task<string> CreateDownloadUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn);
        var expires = expiresAt.ToUnixTimeSeconds();
        var signature = Sign(storagePath, expires);
        var url =
            $"{options.PublicBaseUrl.TrimEnd('/')}/api/v1/files/download" +
            $"?path={Uri.EscapeDataString(storagePath)}" +
            $"&expires={expires}" +
            $"&signature={Uri.EscapeDataString(signature)}";

        return Task.FromResult(url);
    }

    public bool ValidateDownloadUrl(string storagePath, long expiresUnixSeconds, string signature)
    {
        DateTimeOffset expiresAt;
        try
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        if (expiresAt < DateTimeOffset.UtcNow)
        {
            return false;
        }

        var expected = Sign(storagePath, expiresUnixSeconds);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    private string ResolvePath(string storagePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.LocalRootPath));
        var combined = Path.GetFullPath(Path.Combine(root, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(root, combined);
        if (Path.IsPathRooted(relative) ||
            relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage path escapes the configured storage root.");
        }

        return combined;
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Where(character => !invalid.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "files" : cleaned;
    }

    private string Sign(string storagePath, long expiresUnixSeconds)
    {
        var key = string.IsNullOrWhiteSpace(options.SigningKey)
            ? "DEV_ONLY_FileStorageSigningKey_ReplaceInProduction"
            : options.SigningKey;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{storagePath}:{expiresUnixSeconds}"));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

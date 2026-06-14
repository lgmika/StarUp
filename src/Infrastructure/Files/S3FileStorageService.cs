using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Files.Interfaces;

namespace StartupConnect.Infrastructure.Files;

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly FileStorageOptions options;
    private readonly IAmazonS3 client;

    public S3FileStorageService(IOptions<FileStorageOptions> optionsAccessor)
    {
        options = optionsAccessor.Value;
        client = CreateClient(options.S3);
    }

    public async Task<StoredFileResult> UploadAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string category,
        CancellationToken cancellationToken)
    {
        ValidateS3Options();

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storagePath = BuildStoragePath(category, storedFileName);

        var request = new PutObjectRequest
        {
            BucketName = options.S3.BucketName,
            Key = storagePath,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            ServerSideEncryptionMethod = options.S3.UseServerSideEncryption
                ? ServerSideEncryptionMethod.AES256
                : ServerSideEncryptionMethod.None
        };
        request.Metadata["original-file-name"] = Path.GetFileName(originalFileName);
        request.Metadata["content-length"] = sizeInBytes.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await client.PutObjectAsync(request, cancellationToken);

        return new StoredFileResult(
            Path.GetFileName(originalFileName),
            storedFileName,
            storagePath,
            contentType,
            sizeInBytes);
    }

    public async Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        ValidateS3Options();
        var response = await client.GetObjectAsync(options.S3.BucketName, NormalizeStoragePath(storagePath), cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        ValidateS3Options();
        await client.DeleteObjectAsync(options.S3.BucketName, NormalizeStoragePath(storagePath), cancellationToken);
    }

    public Task<string> CreateDownloadUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        ValidateS3Options();
        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.S3.BucketName,
            Key = NormalizeStoragePath(storagePath),
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        };

        return Task.FromResult(client.GetPreSignedURL(request));
    }

    public bool ValidateDownloadUrl(string storagePath, long expiresUnixSeconds, string signature)
    {
        return false;
    }

    private static IAmazonS3 CreateClient(S3FileStorageOptions s3)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = s3.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(s3.ServiceUrl))
        {
            config.ServiceURL = s3.ServiceUrl;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region);
        }

        if (!string.IsNullOrWhiteSpace(s3.AccessKeyId) && !string.IsNullOrWhiteSpace(s3.SecretAccessKey))
        {
            return new AmazonS3Client(new BasicAWSCredentials(s3.AccessKeyId, s3.SecretAccessKey), config);
        }

        return new AmazonS3Client(config);
    }

    private string BuildStoragePath(string category, string storedFileName)
    {
        var prefix = NormalizePrefix(options.S3.KeyPrefix);
        var safeCategory = SanitizeSegment(category);
        var relative = $"{safeCategory}/{DateTimeOffset.UtcNow:yyyy/MM}/{storedFileName}";
        return string.IsNullOrWhiteSpace(prefix) ? relative : $"{prefix}/{relative}";
    }

    private static string NormalizeStoragePath(string storagePath)
    {
        var normalized = storagePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage path must not contain parent traversal segments.");
        }

        return normalized;
    }

    private static string NormalizePrefix(string prefix)
    {
        return prefix.Replace('\\', '/').Trim('/');
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Where(character => !invalid.Contains(character) && character != '/').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "files" : cleaned;
    }

    private void ValidateS3Options()
    {
        if (string.IsNullOrWhiteSpace(options.S3.BucketName))
        {
            throw new InvalidOperationException("FileStorage:S3:BucketName is required when FileStorage:Provider is S3.");
        }
    }
}

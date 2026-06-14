namespace StartupConnect.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public string Provider { get; set; } = "Local";

    public string LocalRootPath { get; set; } = "storage/private";

    public string PublicBaseUrl { get; set; } = "http://localhost:8080";

    public int SignedUrlMinutes { get; set; } = 10;

    public string SigningKey { get; set; } = string.Empty;

    public long MaxCvBytes { get; set; } = 5 * 1024 * 1024;

    public S3FileStorageOptions S3 { get; set; } = new();
}

public sealed class S3FileStorageOptions
{
    public string BucketName { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";

    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "startupconnect";

    public bool ForcePathStyle { get; set; }

    public bool UseServerSideEncryption { get; set; } = true;
}

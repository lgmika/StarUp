namespace StartupConnect.Application.Files.Dtos;

public sealed record StoredFileResult(
    string OriginalFileName,
    string StoredFileName,
    string StoragePath,
    string ContentType,
    long SizeInBytes);

public sealed record FileDownloadUrlResponse(
    Guid FileId,
    string Url,
    DateTimeOffset ExpiresAt);

public sealed record FileMetadataDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record FileListResponse(
    IReadOnlyCollection<FileMetadataDto> Items,
    int Total,
    int Page,
    int PageSize);

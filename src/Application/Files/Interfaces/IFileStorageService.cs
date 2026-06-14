using StartupConnect.Application.Files.Dtos;

namespace StartupConnect.Application.Files.Interfaces;

public interface IFileStorageService
{
    Task<StoredFileResult> UploadAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string category,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);

    Task<string> CreateDownloadUrlAsync(string storagePath, TimeSpan expiresIn, CancellationToken cancellationToken);

    bool ValidateDownloadUrl(string storagePath, long expiresUnixSeconds, string signature);
}

using System.Security.Claims;
using StartupConnect.Application.Files.Dtos;

namespace StartupConnect.Application.Files.Interfaces;

public interface IFileService
{
    Task<StoredFileResult> UploadCvAsync(
        ClaimsPrincipal principal,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        CancellationToken cancellationToken);

    Task<FileDownloadUrlResponse> CreateDownloadUrlAsync(
        ClaimsPrincipal principal,
        Guid fileId,
        CancellationToken cancellationToken);

    Task<FileListResponse> GetMyFilesAsync(
        ClaimsPrincipal principal,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        ClaimsPrincipal principal,
        Guid fileId,
        CancellationToken cancellationToken);
}

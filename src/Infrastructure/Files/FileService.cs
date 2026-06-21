using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Files.Interfaces;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Infrastructure.Persistence;
using StartupConnect.Shared.Exceptions;
using StartupConnect.Shared.Responses;

namespace StartupConnect.Infrastructure.Files;

public sealed class FileService(
    AppDbContext dbContext,
    IFileStorageService fileStorageService,
    IOptions<FileStorageOptions> optionsAccessor,
    ISystemSettingReader systemSettingReader) : IFileService
{
    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();
    private const int MaxPageSize = 100;
    private readonly FileStorageOptions options = optionsAccessor.Value;

    public async Task<StoredFileResult> UploadCvAsync(
        ClaimsPrincipal principal,
        Stream content,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        CancellationToken cancellationToken)
    {
        var maxCvBytes = await systemSettingReader.GetInt64Async("FileStorage.MaxCvBytes", options.MaxCvBytes, cancellationToken);
        ValidateCvUpload(originalFileName, contentType, sizeInBytes, maxCvBytes);

        await using var bufferedContent = new MemoryStream((int)sizeInBytes);
        await CopyWithExactLengthAsync(content, bufferedContent, sizeInBytes, cancellationToken);

        bufferedContent.Position = 0;
        await ValidatePdfSignatureAsync(bufferedContent, cancellationToken);
        bufferedContent.Position = 0;

        return await fileStorageService.UploadAsync(
            bufferedContent,
            originalFileName,
            contentType,
            sizeInBytes,
            "cvs",
            cancellationToken);
    }

    public async Task<FileDownloadUrlResponse> CreateDownloadUrlAsync(
        ClaimsPrincipal principal,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var file = await dbContext.Files.FirstOrDefaultAsync(item => item.Id == fileId && !item.IsDeleted, cancellationToken)
            ?? throw new ApiException("File not found", HttpStatusCode.NotFound);

        if (file.OwnerUserId != userId)
        {
            throw new ApiException("You do not have permission to access this file", HttpStatusCode.Forbidden);
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.SignedUrlMinutes);
        var url = await fileStorageService.CreateDownloadUrlAsync(
            file.StoragePath,
            TimeSpan.FromMinutes(options.SignedUrlMinutes),
            cancellationToken);

        return new FileDownloadUrlResponse(file.Id, url, expiresAt);
    }

    public async Task<FileListResponse> GetMyFilesAsync(
        ClaimsPrincipal principal,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var files = dbContext.Files
            .Where(file => file.OwnerUserId == userId && !file.IsDeleted);
        var total = await files.CountAsync(cancellationToken);
        var items = await files
            .OrderByDescending(file => file.CreatedAt)
            .Skip(Pagination.GetOffset(page, pageSize))
            .Take(pageSize)
            .Select(file => Map(file))
            .ToArrayAsync(cancellationToken);

        return new FileListResponse(items, total, page, pageSize);
    }

    public async Task DeleteAsync(
        ClaimsPrincipal principal,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var file = await dbContext.Files.FirstOrDefaultAsync(
            item => item.Id == fileId && item.OwnerUserId == userId && !item.IsDeleted,
            cancellationToken)
            ?? throw new ApiException("File not found", HttpStatusCode.NotFound);

        file.IsDeleted = true;
        file.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            ActorUserId = userId,
            Action = "File.Delete",
            ResourceType = "StoredFile",
            ResourceId = file.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorageService.DeleteAsync(file.StoragePath, cancellationToken);
    }

    private static void ValidateCvUpload(string originalFileName, string contentType, long sizeInBytes, long maxCvBytes)
    {
        if (maxCvBytes is < 4 or > 100 * 1024 * 1024)
        {
            throw new InvalidOperationException("FileStorage:MaxCvBytes must be between 4 bytes and 100 MB.");
        }

        if (sizeInBytes <= 0)
        {
            throw new ValidationException([new ErrorDetail("EmptyFile", "CV file is required", "file")]);
        }

        if (sizeInBytes > maxCvBytes)
        {
            throw new ValidationException([new ErrorDetail("FileTooLarge", $"CV file must be at most {maxCvBytes / 1024 / 1024} MB", "file")]);
        }

        if (string.IsNullOrWhiteSpace(originalFileName) ||
            originalFileName.Length > 255 ||
            originalFileName.Any(char.IsControl))
        {
            throw new ValidationException([new ErrorDetail("InvalidFileName", "File name must be between 1 and 255 characters and contain no control characters", "file")]);
        }

        var extension = Path.GetExtension(originalFileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException([new ErrorDetail("InvalidFileType", "Only PDF CV uploads are allowed", "file")]);
        }

        var fileName = Path.GetFileName(originalFileName);
        if (!string.Equals(fileName, originalFileName, StringComparison.Ordinal) ||
            originalFileName.Contains('/') ||
            originalFileName.Contains('\\'))
        {
            throw new ValidationException([new ErrorDetail("InvalidFileName", "File name must not contain path segments", "file")]);
        }
    }

    private static async Task CopyWithExactLengthAsync(
        Stream source,
        Stream destination,
        long expectedLength,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        long totalRead = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
            if (totalRead > expectedLength)
            {
                throw new ValidationException([new ErrorDetail("FileSizeMismatch", "Uploaded file size does not match its content", "file")]);
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (totalRead != expectedLength)
        {
            throw new ValidationException([new ErrorDetail("FileSizeMismatch", "Uploaded file size does not match its content", "file")]);
        }
    }

    private static async Task ValidatePdfSignatureAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[PdfSignature.Length];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        if (read != PdfSignature.Length || !buffer.SequenceEqual(PdfSignature))
        {
            throw new ValidationException([new ErrorDetail("InvalidFileSignature", "Uploaded file content is not a valid PDF", "file")]);
        }
    }

    private static FileMetadataDto Map(Domain.Entities.StoredFile file)
    {
        return new FileMetadataDto(
            file.Id,
            file.OriginalFileName,
            file.ContentType,
            file.SizeInBytes,
            file.IsDeleted,
            file.CreatedAt,
            file.UpdatedAt);
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst("nameid")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new ApiException("Invalid access token", HttpStatusCode.Unauthorized);
        }

        return userId;
    }
}

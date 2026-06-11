namespace StartupConnect.Domain.Entities;

public sealed class StoredFile : BaseEntity
{
    public Guid OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public bool IsDeleted { get; set; }
}


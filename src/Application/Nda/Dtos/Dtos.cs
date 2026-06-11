namespace StartupConnect.Application.Nda.Dtos;

public sealed record NdaTemplateDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    IReadOnlyCollection<NdaTemplateVersionDto> Versions);

public sealed record NdaTemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    string Content,
    bool IsPublished,
    DateTimeOffset CreatedAt);

public sealed record CreateNdaTemplateRequest(
    string Name,
    string Description,
    string InitialContent);

public sealed record CreateNdaTemplateVersionRequest(string Content);

public sealed record CurrentProjectNdaDto(
    Guid ProjectId,
    bool RequiresNda,
    Guid? TemplateId,
    Guid? TemplateVersionId,
    int? VersionNumber,
    string? Content,
    bool AlreadyAccepted);

public sealed record NdaAgreementDto(
    Guid Id,
    Guid ProjectId,
    Guid UserId,
    Guid TemplateId,
    Guid TemplateVersionId,
    int VersionNumber,
    DateTimeOffset AcceptedAt,
    string? IpAddress,
    string? UserAgent);


using System.Security.Claims;
using StartupConnect.Application.Nda.Dtos;

namespace StartupConnect.Application.Nda.Interfaces;

public interface INdaService
{
    Task<IReadOnlyCollection<NdaTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken);

    Task<NdaTemplateDto> CreateTemplateAsync(ClaimsPrincipal principal, CreateNdaTemplateRequest request, CancellationToken cancellationToken);

    Task<NdaTemplateVersionDto> CreateTemplateVersionAsync(ClaimsPrincipal principal, Guid templateId, CreateNdaTemplateVersionRequest request, CancellationToken cancellationToken);

    Task<CurrentProjectNdaDto> GetCurrentProjectNdaAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<NdaAgreementDto> AcceptProjectNdaAsync(ClaimsPrincipal principal, Guid projectId, string? ipAddress, string? userAgent, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NdaAgreementDto>> GetProjectAgreementsAsync(ClaimsPrincipal principal, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<NdaAgreementDto>> GetMyAgreementsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}


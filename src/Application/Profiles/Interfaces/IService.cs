using System.Security.Claims;
using StartupConnect.Application.Files.Dtos;
using StartupConnect.Application.Profiles.Dtos;

namespace StartupConnect.Application.Profiles.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetMyProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ProfileDto> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<ProfileDto> CreateProfileAsync(ClaimsPrincipal principal, UpsertProfileRequest request, CancellationToken cancellationToken);

    Task<ProfileDto> UpdateProfileAsync(ClaimsPrincipal principal, UpsertProfileRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken);

    Task<SkillDto> AddUserSkillAsync(ClaimsPrincipal principal, AddUserSkillRequest request, CancellationToken cancellationToken);

    Task RemoveUserSkillAsync(ClaimsPrincipal principal, Guid skillId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CvDto>> GetMyCvsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<CvDto> CreateCvAsync(ClaimsPrincipal principal, CreateCvRequest request, CancellationToken cancellationToken);

    Task<CvDto> CreateUploadedCvAsync(
        ClaimsPrincipal principal,
        StoredFileResult fileResult,
        CancellationToken cancellationToken);

    Task<CvDto> UpdateCvAsync(ClaimsPrincipal principal, Guid cvId, UpdateCvRequest request, CancellationToken cancellationToken);

    Task DeleteCvAsync(ClaimsPrincipal principal, Guid cvId, CancellationToken cancellationToken);

    Task<PortfolioDto> CreatePortfolioAsync(ClaimsPrincipal principal, CreatePortfolioRequest request, CancellationToken cancellationToken);
}

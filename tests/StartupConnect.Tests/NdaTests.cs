using StartupConnect.Application.Nda.Dtos;

namespace StartupConnect.Tests;

public sealed class NdaDtoTests
{
    [Fact]
    public void CurrentProjectNdaDto_Should_Represent_Required_Nda()
    {
        var projectId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var nda = new CurrentProjectNdaDto(
            projectId,
            true,
            templateId,
            versionId,
            2,
            "Confidential content",
            false);

        Assert.True(nda.RequiresNda);
        Assert.Equal(projectId, nda.ProjectId);
        Assert.Equal(templateId, nda.TemplateId);
        Assert.Equal(versionId, nda.TemplateVersionId);
        Assert.Equal(2, nda.VersionNumber);
        Assert.False(nda.AlreadyAccepted);
    }
}

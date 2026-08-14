using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxNcfPreviewPathsTests
{
    [TestMethod]
    public void NcfPreviewTemplate_ShouldBeInteractiveAndBoundToItsOwnPort()
    {
        Assert.IsTrue(SandboxTemplateCatalog.TryGet(SandboxTemplateKeys.NcfPreview, out var template));
        Assert.IsTrue(template.Interactive);
        Assert.AreEqual(8080, template.ContainerPort);
    }

    [TestMethod]
    public void PreviewPath_ShouldRoundTripSessionAndRemainingPath()
    {
        Assert.AreEqual("/sandbox-preview/abc123/", SandboxNcfPreviewPaths.GetEntryUrl("AbC123"));
        Assert.IsTrue(SandboxNcfPreviewPaths.TryParse("/sandbox-preview/abc123/Admin/Index", out var id, out var remaining));
        Assert.AreEqual("abc123", id);
        Assert.AreEqual("/Admin/Index", remaining);
    }
}

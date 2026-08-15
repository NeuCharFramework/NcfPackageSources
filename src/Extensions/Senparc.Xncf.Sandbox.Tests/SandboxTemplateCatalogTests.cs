using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxTemplateCatalogTests
{
    [TestMethod]
    public void JupyterCsharp_IsInteractiveAndUsesDedicatedImage()
    {
        Assert.IsTrue(SandboxTemplateCatalog.TryGet(SandboxTemplateKeys.JupyterCsharp, out var template));
        Assert.IsTrue(template.Interactive);
        Assert.AreEqual("ncf-jupyter-dotnet:10.0", template.Image);
        Assert.AreEqual(8888, template.ContainerPort);
    }
}

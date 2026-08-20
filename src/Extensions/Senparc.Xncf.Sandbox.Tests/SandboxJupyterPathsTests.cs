using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxJupyterPathsTests
{
    [TestMethod]
    public void GetBaseUrl_UsesPrefixAndTrailingSlash()
    {
        var url = SandboxJupyterPaths.GetBaseUrl("AbC123");
        Assert.AreEqual("/sandbox-jupyter/abc123/", url);
        Assert.AreEqual("/sandbox-jupyter/abc123/lab", SandboxJupyterPaths.GetLabEntryUrl("AbC123"));
    }

    [TestMethod]
    public void TryParse_ExtractsSessionAndRemaining()
    {
        var ok = SandboxJupyterPaths.TryParse("/sandbox-jupyter/deadbeef/lab/tree", out var sessionId, out var remaining);
        Assert.IsTrue(ok);
        Assert.AreEqual("deadbeef", sessionId);
        Assert.AreEqual("/lab/tree", remaining);
    }

    [TestMethod]
    public void TryParse_RejectsOtherPaths()
    {
        Assert.IsFalse(SandboxJupyterPaths.TryParse("/Admin/Sandbox", out _, out _));
    }
}

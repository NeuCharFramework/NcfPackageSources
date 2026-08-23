using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxWorkspacePathsTests
{
    [TestMethod]
    public void NormalizeRelativePath_UsesForwardSlashes()
    {
        Assert.AreEqual("data/input.json", SandboxWorkspacePaths.NormalizeRelativePath(@"data\input.json"));
        Assert.AreEqual(string.Empty, SandboxWorkspacePaths.NormalizeRelativePath(null, allowEmpty: true));
    }

    [TestMethod]
    public void NormalizeRelativePath_RejectsTraversalAndRootedPaths()
    {
        Assert.ThrowsException<InvalidOperationException>(() => SandboxWorkspacePaths.NormalizeRelativePath("../secret.txt"));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxWorkspacePaths.NormalizeRelativePath("data/../secret.txt"));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxWorkspacePaths.NormalizeRelativePath("/etc/passwd"));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxWorkspacePaths.NormalizeRelativePath("C:/temp/file.txt"));
    }

    [TestMethod]
    public void CombineContainerPath_StaysUnderConfiguredMount()
    {
        Assert.AreEqual(
            "/home/jovyan/work/data/input.json",
            SandboxWorkspacePaths.CombineContainerPath("/home/jovyan/work", "data/input.json"));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Application.AppServices;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxFileTreeTests
{
    [TestMethod]
    public void BuildWorkspaceFileTree_FormatsNestedFilesAsTree()
    {
        var files = new[]
        {
            new SandboxWorkspaceFileInfo
            {
                RelativePath = "data/input.json",
                Length = 12
            },
            new SandboxWorkspaceFileInfo
            {
                RelativePath = "data/nested/run.cs",
                Length = 24
            },
            new SandboxWorkspaceFileInfo
            {
                RelativePath = "main.py",
                Length = 8
            }
        };

        var tree = SandboxAppService.BuildWorkspaceFileTree(files, null);
        var result = SandboxAppService.FormatWorkspaceFileTree(tree);

        Assert.AreEqual(
            string.Join(
                Environment.NewLine,
                "./",
                "|-- data/",
                "|   |-- nested/",
                "|   |   `-- run.cs",
                "|   `-- input.json",
                "`-- main.py",
                string.Empty),
            result);
    }

    [TestMethod]
    public void BuildWorkspaceFileTree_StripsRequestedDirectoryFromChildren()
    {
        var files = new[]
        {
            new SandboxWorkspaceFileInfo
            {
                RelativePath = "data/input.json"
            }
        };

        var tree = SandboxAppService.BuildWorkspaceFileTree(files, "data");

        Assert.AreEqual("data", tree.Name);
        Assert.AreEqual("input.json", tree.Children![0].Name);
        Assert.AreEqual("file", tree.Children[0].Type);
    }
}

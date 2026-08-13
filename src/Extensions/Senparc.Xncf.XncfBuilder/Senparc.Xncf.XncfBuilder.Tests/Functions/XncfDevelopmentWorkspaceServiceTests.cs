/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentWorkspaceServiceTests.cs
    文件功能描述：隔离开发工作区快照与写入边界测试

    创建标识：Senparc - 20260814

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.XncfBuilder.Domain.Services.Workspace;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class XncfDevelopmentWorkspaceServiceTests
    {
        [TestMethod]
        public async Task CreateSnapshotAsync_ShouldExcludeSecretsAndBuildArtifacts()
        {
            var layout = CreateLayout();
            var jobId = "test-" + Guid.NewGuid().ToString("N");
            try
            {
                var snapshot = await XncfDevelopmentWorkspaceService.CreateSnapshotAsync(layout.SolutionPath, jobId);
                Assert.IsTrue(File.Exists(snapshot.WorkspaceSolutionFilePath));
                Assert.IsTrue(File.Exists(Path.Combine(snapshot.WorkspaceRootPath, "Senparc.Web", "Program.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(snapshot.WorkspaceRootPath, layout.ModuleName, "Domain", "Sample.cs")));
                Assert.IsFalse(File.Exists(Path.Combine(snapshot.WorkspaceRootPath, "Senparc.Web", "appsettings.json")));
                Assert.IsFalse(File.Exists(Path.Combine(snapshot.WorkspaceRootPath, "Senparc.Web", "obj", "project.assets.json")));
                Assert.IsFalse(Directory.Exists(Path.Combine(snapshot.WorkspaceRootPath, ".git")));
            }
            finally
            {
                XncfDevelopmentWorkspaceService.TryDeleteWorkspace(jobId);
                Directory.Delete(layout.Root, recursive: true);
            }
        }

        [TestMethod]
        public void ValidateWritableCodeFile_ShouldRejectBuildAndConfigurationFiles()
        {
            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                XncfWorkspaceFileService.ValidateWritableCodeFile("Senparc.Xncf.Sample.csproj"));
            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                XncfWorkspaceFileService.ValidateWritableCodeFile("appsettings.json"));
            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                XncfWorkspaceFileService.ValidateWritableCodeFile("Directory.Build.props"));

            XncfWorkspaceFileService.ValidateWritableCodeFile(Path.Combine("Domain", "Sample.cs"));
            XncfWorkspaceFileService.ValidateWritableCodeFile(Path.Combine("Areas", "Admin", "Index.cshtml"));
        }

        private static TestLayout CreateLayout()
        {
            const string moduleName = "Demo.Xncf.Sample";
            var root = Path.Combine(Path.GetTempPath(), "NcfXncfDevelopmentWorkspaceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, ".git"));
            Directory.CreateDirectory(Path.Combine(root, "Senparc.Web", "obj"));
            Directory.CreateDirectory(Path.Combine(root, moduleName, "Domain"));
            var solution = Path.Combine(root, "Demo.sln");
            File.WriteAllText(solution,
                "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Senparc.Web\", \"Senparc.Web\\Senparc.Web.csproj\", \"{00000000-0000-0000-0000-000000000001}\"\nEndProject\n" +
                "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Demo\", \"Demo.Xncf.Sample\\Demo.Xncf.Sample.csproj\", \"{00000000-0000-0000-0000-000000000002}\"\nEndProject\n");
            File.WriteAllText(Path.Combine(root, "Senparc.Web", "Senparc.Web.csproj"),
                "<Project><ItemGroup><ProjectReference Include=\"..\\Demo.Xncf.Sample\\Demo.Xncf.Sample.csproj\" /></ItemGroup></Project>");
            File.WriteAllText(Path.Combine(root, "Senparc.Web", "Program.cs"), "// host");
            File.WriteAllText(Path.Combine(root, "Senparc.Web", "appsettings.json"), "{ \"secret\": true }");
            File.WriteAllText(Path.Combine(root, "Senparc.Web", "obj", "project.assets.json"), "ignored");
            File.WriteAllText(Path.Combine(root, moduleName, moduleName + ".csproj"), "<Project />");
            File.WriteAllText(Path.Combine(root, moduleName, "Domain", "Sample.cs"), "public class Sample { }");
            return new TestLayout(root, solution, moduleName);
        }

        private sealed record TestLayout(string Root, string SolutionPath, string ModuleName);
    }
}

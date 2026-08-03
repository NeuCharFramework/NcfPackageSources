/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfWorkspaceFileServiceTests.cs
    文件功能描述：XNCF 工作区路径约束、原子写入和指纹并发测试


    创建标识：Senparc - 20260802

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.XncfBuilder.Domain.Services.Workspace;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class XncfWorkspaceFileServiceTests
    {
        [TestMethod]
        public void ResolveFilePath_ShouldRejectPathsOutsideModule()
        {
            var layout = CreateWorkspace();
            try
            {
                var moduleDirectory = XncfWorkspaceFileService.ResolveModuleDirectory(
                    layout.SolutionFilePath,
                    layout.ModuleName);

                Assert.ThrowsException<UnauthorizedAccessException>(() =>
                    XncfWorkspaceFileService.ResolveFilePath(moduleDirectory, "../outside.cs"));
                Assert.ThrowsException<ArgumentException>(() =>
                    XncfWorkspaceFileService.ResolveFilePath(moduleDirectory, Path.GetFullPath("outside.cs")));
            }
            finally
            {
                Directory.Delete(layout.RootDirectory, recursive: true);
            }
        }

        [TestMethod]
        public async Task WriteTextAtomicAsync_ShouldUseSha256ForOptimisticConcurrency()
        {
            var layout = CreateWorkspace();
            try
            {
                var moduleDirectory = XncfWorkspaceFileService.ResolveModuleDirectory(
                    layout.SolutionFilePath,
                    layout.ModuleName);
                var relativeFilePath = Path.Combine("Domain", "Sample.cs");

                var initialWrite = await XncfWorkspaceFileService.WriteTextAtomicAsync(
                    moduleDirectory,
                    relativeFilePath,
                    "initial");
                Assert.IsTrue(initialWrite.IsNewFile);

                var read = await XncfWorkspaceFileService.ReadTextAsync(moduleDirectory, relativeFilePath);
                Assert.AreEqual("initial", read.Content);
                Assert.AreEqual(initialWrite.Sha256, read.Sha256);

                var update = await XncfWorkspaceFileService.WriteTextAtomicAsync(
                    moduleDirectory,
                    relativeFilePath,
                    "updated",
                    read.Sha256);
                Assert.IsFalse(update.IsNewFile);
                Assert.AreEqual(read.Sha256, update.PreviousSha256);

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                    XncfWorkspaceFileService.WriteTextAtomicAsync(
                        moduleDirectory,
                        relativeFilePath,
                        "stale update",
                        read.Sha256));

                Assert.AreEqual(
                    "updated",
                    (await XncfWorkspaceFileService.ReadTextAsync(moduleDirectory, relativeFilePath)).Content);
            }
            finally
            {
                Directory.Delete(layout.RootDirectory, recursive: true);
            }
        }

        private static WorkspaceLayout CreateWorkspace()
        {
            const string moduleName = "Demo.Xncf.Sample";
            var rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "NcfXncfWorkspaceTests",
                Guid.NewGuid().ToString("N"));
            var moduleDirectory = Path.Combine(rootDirectory, moduleName);
            Directory.CreateDirectory(moduleDirectory);

            var solutionFilePath = Path.Combine(rootDirectory, "Demo.sln");
            File.WriteAllText(solutionFilePath, string.Empty);
            File.WriteAllText(
                Path.Combine(moduleDirectory, $"{moduleName}.csproj"),
                "<Project />");

            return new WorkspaceLayout(rootDirectory, solutionFilePath, moduleName);
        }

        private sealed record WorkspaceLayout(
            string RootDirectory,
            string SolutionFilePath,
            string ModuleName);
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewServiceTests.cs
    文件功能描述：XNCF 独立预览路径、命令和隔离配置测试


    创建标识：Senparc - 20260801

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using System;
using System.IO;
using System.Linq;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class XncfPreviewServiceTests
    {
        [TestMethod]
        public void ResolveProjectPaths_ShouldUseBuilderConvention()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                var paths = XncfPreviewService.ResolveProjectPaths(
                    Path.Combine(testDirectory, "Demo.sln"),
                    "Demo.Xncf.Sample");

                Assert.AreEqual(
                    Path.Combine(testDirectory, "Senparc.Web", "Senparc.Web.csproj"),
                    paths.WebProjectFilePath);
                Assert.AreEqual(
                    Path.Combine(testDirectory, "Demo.Xncf.Sample", "Demo.Xncf.Sample.csproj"),
                    paths.ModuleProjectFilePath);
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void ResolveProjectPaths_ShouldRejectDirectoryTraversal()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    XncfPreviewService.ResolveProjectPaths(
                        Path.Combine(testDirectory, "Demo.sln"),
                        "../Demo.Xncf.Sample"));
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void CreatePublishStartInfo_ShouldUseIsolatedOutputWithoutRestore()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), "Senparc.Web", "Senparc.Web.csproj");
            var publishPath = Path.Combine(Path.GetTempPath(), "NcfPreview", "app");

            var startInfo = XncfPreviewService.CreatePublishStartInfo(projectPath, publishPath);
            var arguments = startInfo.ArgumentList.ToArray();

            Assert.AreEqual("dotnet", startInfo.FileName);
            CollectionAssert.Contains(arguments, "publish");
            CollectionAssert.Contains(arguments, "--no-restore");
            CollectionAssert.Contains(arguments, "--disable-build-servers");
            CollectionAssert.Contains(arguments, "-m:1");
            CollectionAssert.Contains(arguments, publishPath);
            Assert.IsTrue(startInfo.RedirectStandardOutput);
            Assert.IsTrue(startInfo.RedirectStandardError);
        }

        [TestMethod]
        public void CreateRestoreStartInfo_ShouldRestoreOnlyTheWebProjectSerially()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), "Senparc.Web", "Senparc.Web.csproj");
            var startInfo = XncfPreviewService.CreateRestoreStartInfo(projectPath);
            var arguments = startInfo.ArgumentList.ToArray();

            CollectionAssert.AreEqual(
                new[] { "restore", projectPath, "--disable-build-servers", "-m:1" },
                arguments);
        }

        [TestMethod]
        public void RequiresRestore_ShouldDetectMissingOrStaleAssets()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                var paths = XncfPreviewService.ResolveProjectPaths(
                    Path.Combine(testDirectory, "Demo.sln"),
                    "Demo.Xncf.Sample");
                Assert.IsTrue(XncfPreviewService.RequiresRestore(paths));

                WriteCurrentAssetsFile(paths.WebProjectFilePath);
                WriteCurrentAssetsFile(paths.ModuleProjectFilePath);
                Assert.IsFalse(XncfPreviewService.RequiresRestore(paths));

                File.SetLastWriteTimeUtc(paths.ModuleProjectFilePath, DateTime.UtcNow.AddMinutes(1));
                Assert.IsTrue(XncfPreviewService.RequiresRestore(paths));
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void CreateWebStartInfo_DefaultPreview_ShouldUseLoopbackAndIsolatedDatabase()
        {
            var publishPath = Path.Combine(Path.GetTempPath(), "NcfPreview", "app");
            var startInfo = XncfPreviewService.CreateWebStartInfo(
                publishPath,
                5088,
                XncfPreviewService.DefaultEnvironmentName);

            CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "--urls=http://127.0.0.1:5088");
            Assert.AreEqual("http://127.0.0.1:5088", startInfo.Environment["ASPNETCORE_URLS"]);
            Assert.AreEqual("XncfPreview", startInfo.Environment["ASPNETCORE_ENVIRONMENT"]);
            Assert.AreEqual("false", startInfo.Environment["DOTNET_hostBuilder__reloadConfigOnChange"]);
            Assert.AreEqual("1", startInfo.Environment["DOTNET_USE_POLLING_FILE_WATCHER"]);
            Assert.AreEqual("Local", startInfo.Environment["SenparcCoreSetting__DatabaseName"]);
            Assert.AreEqual("Sqlite", startInfo.Environment["SenparcCoreSetting__DatabaseType"]);
            Assert.AreEqual("Local", startInfo.Environment["SenparcCoreSetting__CacheType"]);
        }

        [TestMethod]
        public void CreateWebStartInfo_CustomEnvironment_ShouldNotOverrideDatabaseSelection()
        {
            var startInfo = XncfPreviewService.CreateWebStartInfo(
                Path.GetTempPath(),
                5089,
                "Development");

            Assert.IsFalse(startInfo.Environment.ContainsKey("SenparcCoreSetting__DatabaseName"));
            Assert.IsFalse(startInfo.Environment.ContainsKey("SenparcCoreSetting__DatabaseType"));
        }

        private static string CreateProjectLayout(string moduleProjectName)
        {
            var testDirectory = Path.Combine(
                Path.GetTempPath(),
                "NcfXncfPreviewTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(testDirectory, "Senparc.Web"));
            Directory.CreateDirectory(Path.Combine(testDirectory, moduleProjectName));
            File.WriteAllText(Path.Combine(testDirectory, "Demo.sln"), string.Empty);
            File.WriteAllText(
                Path.Combine(testDirectory, "Senparc.Web", "Senparc.Web.csproj"),
                "<Project />");
            File.WriteAllText(
                Path.Combine(testDirectory, moduleProjectName, $"{moduleProjectName}.csproj"),
                "<Project />");
            return testDirectory;
        }

        private static void WriteCurrentAssetsFile(string projectFilePath)
        {
            var objDirectory = Path.Combine(Path.GetDirectoryName(projectFilePath), "obj");
            Directory.CreateDirectory(objDirectory);
            var assetsFilePath = Path.Combine(objDirectory, "project.assets.json");
            File.WriteAllText(assetsFilePath, "{}");
            File.SetLastWriteTimeUtc(projectFilePath, DateTime.UtcNow.AddMinutes(-1));
            File.SetLastWriteTimeUtc(assetsFilePath, DateTime.UtcNow);
        }
    }
}

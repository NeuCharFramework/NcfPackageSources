/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：BuildXncfAppServiceTests.cs
    文件功能描述：BuildXncfAppService 跨平台命令执行及失败响应测试


    创建标识：Senparc - 20260725
    创建描述：验证 XNCF 生成命令无需 cmd.exe，并防止失败时返回成功信息

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.XncfBuilder.OHS.Local;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class BuildXncfAppServiceTests
    {
        [TestMethod]
        public async Task ExecuteDotNetCommandAsync_ShouldRunWithoutPlatformShell()
        {
            var result = await BuildXncfAppService.ExecuteDotNetCommandAsync(
                Path.GetTempPath(),
                new[] { "--version" });

            Assert.IsTrue(result.Started);
            Assert.AreEqual(0, result.ExitCode, result.StandardError);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.StandardOutput));
        }

        [TestMethod]
        public async Task ExecuteDotNetCommandAsync_ShouldCaptureNonZeroExitCode()
        {
            var result = await BuildXncfAppService.ExecuteDotNetCommandAsync(
                Path.GetTempPath(),
                new[] { "__xncf_builder_invalid_command__" });

            Assert.IsTrue(result.Started);
            Assert.AreNotEqual(0, result.ExitCode);
            Assert.IsTrue(
                !string.IsNullOrWhiteSpace(result.StandardOutput)
                || !string.IsNullOrWhiteSpace(result.StandardError));
        }

        [TestMethod]
        public async Task Build_ShouldReturnFailureAndNotCreateBackup_WhenRequiredWebProjectIsMissing()
        {
            var testDirectory = Path.Combine(Path.GetTempPath(), "NcfXncfBuilderTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);

            try
            {
                var solutionFilePath = Path.Combine(testDirectory, "Test.sln");
                File.WriteAllText(solutionFilePath, string.Empty);

                using var serviceProvider = new ServiceCollection().BuildServiceProvider();
                var appService = new BuildXncfAppService(serviceProvider, null, null, null);
                var response = await appService.Build(new BuildXncf_BuildRequest
                {
                    SlnFilePath = solutionFilePath,
                    NewSlnFile = new[] { "backup" },
                    TemplatePackage = "no",
                    FrameworkVersion = "net8.0",
                    OrgName = "Senparc",
                    XncfName = "MissingWebProject",
                    Version = "1.0.0",
                    MenuName = "Test",
                    Icon = "fa fa-test",
                    Description = "Test"
                });

                Assert.AreEqual(false, response.Success);
                StringAssert.Contains(response.Data, "项目生成失败");
                StringAssert.Contains(response.Data, "Senparc.Web.csproj");
                Assert.IsFalse(Directory.EnumerateFiles(testDirectory, "*-backup-*.sln").Any());
                Assert.IsFalse(Directory.EnumerateFiles(testDirectory, "*-new-*.sln").Any());
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}

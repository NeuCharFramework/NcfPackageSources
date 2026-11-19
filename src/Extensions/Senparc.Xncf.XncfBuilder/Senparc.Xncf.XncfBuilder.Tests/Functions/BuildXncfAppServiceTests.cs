/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BuildXncfAppServiceTests.cs
    File description: Related implementation.

    Created by: Senparc - 20260725
    Creation description: Related implementation.

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

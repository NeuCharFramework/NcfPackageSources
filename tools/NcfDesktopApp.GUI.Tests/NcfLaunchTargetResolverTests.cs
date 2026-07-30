using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class NcfLaunchTargetResolverTests
{
    private string _testRoot = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ncf-launch-target-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [TestMethod]
    public void ResolveExternal_WhenPublishedDirectorySelected_ReadsVersionAndFramework()
    {
        File.WriteAllBytes(Path.Combine(_testRoot, "Senparc.Web.dll"), Array.Empty<byte>());
        File.WriteAllText(Path.Combine(_testRoot, "version.txt"), "v0.34.0-test");
        File.WriteAllText(
            Path.Combine(_testRoot, "Senparc.Web.runtimeconfig.json"),
            """
            {
              "runtimeOptions": {
                "tfm": "net10.0"
              }
            }
            """);

        var result = NcfLaunchTargetResolver.ResolveExternal(_testRoot);

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(NcfLaunchTargetKind.ExternalPublished, result.Target!.Kind);
        Assert.AreEqual("v0.34.0-test", result.Target.Version);
        Assert.AreEqual("net10.0", result.Target.TargetFramework);
        Assert.AreEqual(_testRoot, result.Target.WorkingDirectory);
    }

    [TestMethod]
    public void ResolveExternal_WhenRepositoryRootSelected_FindsSourceProjectWithoutScanningBin()
    {
        var projectDirectory = Path.Combine(_testRoot, "tools", "NcfSite", "Senparc.Web");
        Directory.CreateDirectory(projectDirectory);
        var projectPath = Path.Combine(projectDirectory, "Senparc.Web.csproj");
        File.WriteAllText(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <Version>0.29.1</Version>
              </PropertyGroup>
            </Project>
            """);
        var ignoredBin = Path.Combine(_testRoot, "bin", "Debug", "net9.0");
        Directory.CreateDirectory(ignoredBin);
        File.WriteAllBytes(Path.Combine(ignoredBin, "Senparc.Web.dll"), Array.Empty<byte>());

        var result = NcfLaunchTargetResolver.ResolveExternal(_testRoot);

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(NcfLaunchTargetKind.SourceProject, result.Target!.Kind);
        Assert.AreEqual(projectPath, result.Target.EntryPath);
        Assert.AreEqual("0.29.1", result.Target.Version);
        Assert.AreEqual("net9.0", result.Target.TargetFramework);
    }

    [TestMethod]
    public void ResolveManagedRuntime_WhenPackageHasNestedDirectory_FindsPublishedEntry()
    {
        var appDirectory = Path.Combine(_testRoot, "package", "app");
        Directory.CreateDirectory(appDirectory);
        File.WriteAllBytes(Path.Combine(appDirectory, "Senparc.Web.dll"), Array.Empty<byte>());
        File.WriteAllText(Path.Combine(_testRoot, "version.txt"), "v0.30.0-nested");

        var result = NcfLaunchTargetResolver.ResolveManagedRuntime(_testRoot);

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(NcfLaunchTargetKind.ManagedPublished, result.Target!.Kind);
        Assert.AreEqual(appDirectory, result.Target.WorkingDirectory);
        Assert.AreEqual("v0.30.0-nested", result.Target.Version);
    }

    [TestMethod]
    public void ResolveExternal_WhenDirectoryHasNoEntry_ReturnsActionableError()
    {
        var result = NcfLaunchTargetResolver.ResolveExternal(_testRoot);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.ErrorMessage, "Senparc.Web");
    }

    [TestMethod]
    public void ResolveRemote_WhenHttpsAddressProvided_ReturnsRemoteTarget()
    {
        var result = NcfLaunchTargetResolver.ResolveRemote("https://ncf.example.com/root/");

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(NcfLaunchTargetKind.RemoteSite, result.Target!.Kind);
        Assert.AreEqual("https://ncf.example.com/root", result.Target.EntryPath);
        Assert.IsTrue(result.Target.IsRemoteSite);
    }

    [TestMethod]
    public void ResolveRemote_WhenPlainHttpIsNotLoopback_ReturnsSecurityError()
    {
        var result = NcfLaunchTargetResolver.ResolveRemote("http://10.0.0.8:5000");

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.ErrorMessage, "HTTPS");
    }

    [TestMethod]
    public void ResolveRemote_WhenHttpUsesSshTunnelLoopback_IsAllowed()
    {
        var result = NcfLaunchTargetResolver.ResolveRemote("http://127.0.0.1:5500");

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual("本机隧道/回环 HTTP", result.Target!.TargetFramework);
    }

    [TestMethod]
    public void TemplateWorkspaceValidation_WhenTargetIsNotEmpty_DoesNotAllowOverwrite()
    {
        var target = Path.Combine(_testRoot, "ExistingWorkspace");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "keep.txt"), "user data");

        Assert.ThrowsException<IOException>(() =>
            TemplateWorkspaceService.ValidateAndGetTargetPath(_testRoot, "ExistingWorkspace"));
        Assert.IsTrue(File.Exists(Path.Combine(target, "keep.txt")));
    }

    [TestMethod]
    public void TemplateWorkspaceValidation_WhenNameEscapesParent_IsRejected()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            TemplateWorkspaceService.ValidateAndGetTargetPath(_testRoot, "../escaped"));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class DotnetSdkResolverTests
{
    [TestMethod]
    public void Resolve_WhenGuiPathCannotFindDotnet_UsesAbsoluteCandidateWithRequiredSdk()
    {
        const string absoluteDotnet = "/usr/local/share/dotnet/dotnet";

        var result = DotnetSdkResolver.Resolve(
            "net10.0",
            "/workspace",
            new[] { "dotnet", absoluteDotnet },
            (path, argument, _) => (path, argument) switch
            {
                ("dotnet", _) => DotnetCommandResult.Failed("not found"),
                (absoluteDotnet, "--list-sdks") => Success("8.0.101 [/sdk]\n10.0.101 [/sdk]\n"),
                (absoluteDotnet, "--version") => Success("10.0.101\n"),
                _ => DotnetCommandResult.Failed("unexpected")
            });

        Assert.IsTrue(result.IsValid, result.ErrorMessage);
        Assert.AreEqual(absoluteDotnet, result.DotnetPath);
        Assert.AreEqual(10, result.RequiredMajorVersion);
        Assert.AreEqual("10.0.101", result.SelectedSdkVersion);
    }

    [TestMethod]
    public void Resolve_WhenOnlyOlderSdkExists_ReturnsActionableVersionError()
    {
        var result = DotnetSdkResolver.Resolve(
            "net10.0",
            "/workspace",
            new[] { "/fake/dotnet" },
            (_, argument, _) => argument == "--list-sdks"
                ? Success("8.0.101 [/sdk]\n9.0.202 [/sdk]\n")
                : Success("9.0.202\n"));

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.ErrorMessage, "net10.0");
        StringAssert.Contains(result.ErrorMessage, "9.0.202");
        StringAssert.Contains(result.ErrorMessage, "不会自动安装 SDK");
    }

    [TestMethod]
    public void Resolve_WhenGlobalJsonSelectsOlderSdk_DoesNotClaimCompatibility()
    {
        var result = DotnetSdkResolver.Resolve(
            "net10.0",
            "/workspace",
            new[] { "/fake/dotnet" },
            (_, argument, _) => argument == "--list-sdks"
                ? Success("9.0.202 [/sdk]\n10.0.101 [/sdk]\n")
                : Success("9.0.202\n"));

        Assert.IsFalse(result.IsValid);
        CollectionAssert.Contains(result.DetectedSdkVersions.ToArray(), "10.0.101");
    }

    [DataTestMethod]
    [DataRow("net10.0", 10)]
    [DataRow("net10.0;net9.0", 10)]
    [DataRow(".NETCoreApp,Version=v10.0", 10)]
    [DataRow("netcoreapp8.0", 8)]
    public void GetTargetFrameworkMajorVersion_ParsesSupportedFormats(string framework, int expected)
    {
        Assert.AreEqual(expected, DotnetSdkResolver.GetTargetFrameworkMajorVersion(framework));
    }

    [TestMethod]
    public void CreateSourceProjectStartInfo_UsesResolvedHostAndNeverRestores()
    {
        var target = new NcfLaunchTarget(
            NcfLaunchTargetKind.SourceProject,
            "/workspace",
            "/workspace/Senparc.Web",
            "/workspace/Senparc.Web/Senparc.Web.csproj",
            "Senparc.Web",
            "0.34.0",
            "net10.0");

        var startInfo = NcfService.CreateSourceProjectStartInfo(
            target,
            "/usr/local/share/dotnet/dotnet",
            5001,
            "test-token",
            "Development");

        Assert.AreEqual("/usr/local/share/dotnet/dotnet", startInfo.FileName);
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "--no-restore");
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "--no-launch-profile");
        CollectionAssert.DoesNotContain(startInfo.ArgumentList.ToArray(), "restore");
        Assert.AreEqual("http://localhost:5001", startInfo.Environment["ASPNETCORE_URLS"]);
        Assert.AreEqual("Development", startInfo.Environment["ASPNETCORE_ENVIRONMENT"]);
        Assert.AreEqual("test-token", startInfo.Environment["NCF_DESKTOP_BRIDGE_TOKEN"]);
    }

    [TestMethod]
    public void ApplyDotnetEnvironment_UsesResolvedHostDirectoryForChildOnly()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo();

        NcfService.ApplyDotnetEnvironment(startInfo, "/usr/local/share/dotnet/dotnet");

        Assert.AreEqual("/usr/local/share/dotnet", startInfo.Environment["DOTNET_ROOT"]);
        StringAssert.StartsWith(
            startInfo.Environment["PATH"] ?? string.Empty,
            "/usr/local/share/dotnet" + Path.PathSeparator);
    }

    private static DotnetCommandResult Success(string output) => new(true, output, string.Empty, 0);
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxImageResolverTests
{
    [TestMethod]
    public void InteractiveCreateTimeout_UsesConfiguredSecondsWithinRange()
    {
        var options = new SandboxDockerOptions { InteractiveCreateTimeoutSeconds = 1200 };

        Assert.AreEqual(TimeSpan.FromSeconds(1200), options.GetInteractiveCreateTimeout());
    }

    [TestMethod]
    public void InteractiveCreateTimeout_ClampsInvalidValues()
    {
        Assert.AreEqual(
            TimeSpan.FromSeconds(60),
            new SandboxDockerOptions { InteractiveCreateTimeoutSeconds = 1 }.GetInteractiveCreateTimeout());
        Assert.AreEqual(
            TimeSpan.FromHours(1),
            new SandboxDockerOptions { InteractiveCreateTimeoutSeconds = 7200 }.GetInteractiveCreateTimeout());
    }

    [TestMethod]
    public void Resolve_WithoutPrefix_ReturnsDefault()
    {
        var resolver = new SandboxImageResolver(new SandboxImageOptions());
        var image = resolver.Resolve(SandboxTemplateKeys.PythonExec, "python:3.12-alpine");
        Assert.AreEqual("python:3.12-alpine", image);
    }

    [TestMethod]
    public void Resolve_WithPrefix_PrependsLeaf()
    {
        var resolver = new SandboxImageResolver(new SandboxImageOptions
        {
            RegistryPrefix = "registry.example.com/ncf-sandbox/"
        });
        var image = resolver.Resolve(SandboxTemplateKeys.PythonExec, "python:3.12-alpine");
        Assert.AreEqual("registry.example.com/ncf-sandbox/python:3.12-alpine", image);
    }

    [TestMethod]
    public void Resolve_Override_WinsOverPrefix()
    {
        var resolver = new SandboxImageResolver(new SandboxImageOptions
        {
            RegistryPrefix = "registry.example.com/ncf-sandbox",
            Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SandboxTemplateKeys.CsharpExec] = "registry.example.com/custom/dotnet-sdk:10.0"
            }
        });
        var image = resolver.Resolve(SandboxTemplateKeys.CsharpExec, "mcr.microsoft.com/dotnet/sdk:10.0");
        Assert.AreEqual("registry.example.com/custom/dotnet-sdk:10.0", image);
    }

    [TestMethod]
    public void Resolve_PathDefault_UsesLastSegmentWithPrefix()
    {
        var resolver = new SandboxImageResolver(new SandboxImageOptions
        {
            RegistryPrefix = "registry.example.com/ncf-sandbox"
        });
        var image = resolver.Resolve(SandboxTemplateKeys.CsharpExec, "mcr.microsoft.com/dotnet/sdk:10.0");
        Assert.AreEqual("registry.example.com/ncf-sandbox/sdk:10.0", image);
    }
}

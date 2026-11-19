/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxPortMappingTests.cs
    File description: Related implementation.

    Created by: Senparc - 20260918

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxPortMappingTests
{
    [TestMethod]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        Assert.AreEqual(0, SandboxPortMappings.Parse(null, 8).Count);
        Assert.AreEqual(0, SandboxPortMappings.Parse("  ; , ", 8).Count);
    }

    [TestMethod]
    public void Parse_SingleContainerPort_AutoHostLoopback()
    {
        var result = SandboxPortMappings.Parse("3000", 8);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(0, result[0].HostPort);
        Assert.AreEqual(3000, result[0].ContainerPort);
        Assert.IsFalse(result[0].ExposeExternally);
    }

    [TestMethod]
    public void Parse_ExplicitHostPort_Loopback()
    {
        var result = SandboxPortMappings.Parse("9000:3000", 8);
        Assert.AreEqual(9000, result[0].HostPort);
        Assert.AreEqual(3000, result[0].ContainerPort);
        Assert.IsFalse(result[0].ExposeExternally);
    }

    [TestMethod]
    public void Parse_ExternalAutoHost_ExposeExternally()
    {
        var result = SandboxPortMappings.Parse("*:3000", 8);
        Assert.AreEqual(0, result[0].HostPort);
        Assert.AreEqual(3000, result[0].ContainerPort);
        Assert.IsTrue(result[0].ExposeExternally);
    }

    [TestMethod]
    public void Parse_ExternalExplicitHost_ExposeExternally()
    {
        var result = SandboxPortMappings.Parse("*:9000:3000", 8);
        Assert.AreEqual(9000, result[0].HostPort);
        Assert.AreEqual(3000, result[0].ContainerPort);
        Assert.IsTrue(result[0].ExposeExternally);
    }

    [TestMethod]
    public void Parse_MultipleEntries_SemicolonAndComma()
    {
        var result = SandboxPortMappings.Parse("3000, 9000:4000; *:5000", 8);
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(3000, result[0].ContainerPort);
        Assert.AreEqual(9000, result[1].HostPort);
        Assert.IsTrue(result[2].ExposeExternally);
    }

    [TestMethod]
    public void Parse_DuplicateHostPorts_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(
            () => SandboxPortMappings.Parse("9000:3000; 9000:4000", 8));
    }

    [TestMethod]
    public void Parse_InvalidPort_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() => SandboxPortMappings.Parse("0", 8));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxPortMappings.Parse("70000", 8));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxPortMappings.Parse("abc", 8));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxPortMappings.Parse("9000:3000:4000", 8));
    }

    [TestMethod]
    public void Parse_TooManyEntries_Throws()
    {
        var input = string.Join(";", Enumerable.Range(1000, 9).Select(z => z.ToString()));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxPortMappings.Parse(input, 8));
    }

    [TestMethod]
    public void DisplayString_UsesBindingAddress()
    {
        var loopback = new SandboxPortMapping(49152, 3000, false);
        Assert.AreEqual("127.0.0.1:49152:3000", loopback.ToDisplayString());

        var external = new SandboxPortMapping(8080, 3000, true);
        Assert.AreEqual("0.0.0.0:8080:3000", external.ToDisplayString());
    }
}

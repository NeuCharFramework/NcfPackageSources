/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxQuotaPolicyTests.cs
    File description: Related implementation.

    Created by: Senparc - 20260918

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxQuotaPolicyTests
{
    [TestMethod]
    public void Defaults_ReflectRaisedSessionLimits()
    {
        var quota = new SandboxQuotaPolicy();
        Assert.AreEqual(10, quota.MaxSessionsPerUser);
        Assert.AreEqual(50, quota.MaxGlobalSessions);
        Assert.AreEqual(8, quota.MaxExtraPortMappings);
        Assert.IsTrue(quota.MaxInteractiveStdinCharacters >= 8_000);
    }
}

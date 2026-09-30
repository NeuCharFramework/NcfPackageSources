/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxQuotaPolicyTests.cs
    文件功能描述：沙箱配额默认值回归测试

    创建标识：Senparc - 20260918
    修改标识：Senparc - 20260918
    修改描述：v0.3.3 新增配额默认值回归测试

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

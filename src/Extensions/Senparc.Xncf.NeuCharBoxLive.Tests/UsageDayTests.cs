/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：UsageDayTests.cs
    文件功能描述：NeuCharBox Live 测试：UsageDayTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class UsageDayTests
{
    [TestMethod]
    public void UsageDay_ResetsAtEasternMidnight()
    {
        Assert.AreEqual(new DateOnly(2026, 11, 7), UsageDay.For(DateTimeOffset.Parse("2026-11-08T04:59:00Z"))); // EST, UTC−5
        Assert.AreEqual(new DateOnly(2026, 11, 8), UsageDay.For(DateTimeOffset.Parse("2026-11-08T05:00:00Z")));
        Assert.AreEqual(new DateOnly(2026, 7, 1), UsageDay.For(DateTimeOffset.Parse("2026-07-02T03:59:00Z")));  // EDT, UTC−4
    }

    [TestMethod]
    public void UsageDay_IgnoresTheOffsetItIsGiven() =>
        Assert.AreEqual(new DateOnly(2026, 11, 7), UsageDay.For(DateTimeOffset.Parse("2026-11-08T12:59:00+08:00")));
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：UsageDayTests.cs
    文件功能描述：测试使用日：时区、日界与每日计数的归属


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class UsageDayTests
{
    static UsageDay NewYork()
    {
        Assert.IsTrue(UsageDay.TryFromId("America/New_York", out var ny));
        return ny;
    }

    [TestMethod]
    public void UsageDay_Utc_FlipsAtUtcMidnight()
    {
        Assert.AreEqual(new DateOnly(2026, 11, 9), UsageDay.Utc.For(DateTimeOffset.Parse("2026-11-09T00:30Z")));
        Assert.AreEqual(new DateOnly(2026, 11, 8), UsageDay.Utc.For(DateTimeOffset.Parse("2026-11-08T23:59:59Z")));
        Assert.IsTrue(UsageDay.TryFromId("America/New_York", out var ny));
        Assert.AreEqual(new DateOnly(2026, 11, 8), ny.For(DateTimeOffset.Parse("2026-11-09T00:30Z")));
    }

    [TestMethod]
    public void UsageDay_NewYork_ResetsAtEasternMidnight()
    {
        var ny = NewYork();
        Assert.AreEqual(new DateOnly(2026, 11, 7), ny.For(DateTimeOffset.Parse("2026-11-08T04:59:00Z"))); // EST, UTC−5
        Assert.AreEqual(new DateOnly(2026, 11, 8), ny.For(DateTimeOffset.Parse("2026-11-08T05:00:00Z")));
        Assert.AreEqual(new DateOnly(2026, 7, 1), ny.For(DateTimeOffset.Parse("2026-07-02T03:59:00Z")));  // EDT, UTC−4
    }

    [TestMethod]
    public void UsageDay_IgnoresTheOffsetItIsGiven()
    {
        // 12:59 at +08:00 is 04:59 UTC.
        Assert.AreEqual(new DateOnly(2026, 11, 8), UsageDay.Utc.For(DateTimeOffset.Parse("2026-11-08T12:59:00+08:00")));
        Assert.AreEqual(new DateOnly(2026, 11, 7), NewYork().For(DateTimeOffset.Parse("2026-11-08T12:59:00+08:00")));
    }

    [TestMethod]
    public void UsageDay_TryFromId_EmptyIsUtc_UnknownIsFalse()
    {
        foreach (var id in new[] { null, "", "  " })
        {
            Assert.IsTrue(UsageDay.TryFromId(id, out var d), $"'{id}'");
            Assert.AreSame(TimeZoneInfo.Utc, d.Zone, $"'{id}'");
        }
        Assert.IsTrue(UsageDay.TryFromId("UTC", out var utc));
        Assert.AreEqual(TimeSpan.Zero, utc.Zone.BaseUtcOffset);
        foreach (var id in new[] { "Mars/Base", "not a zone", new string('x', 300) })
        {
            Assert.IsFalse(UsageDay.TryFromId(id, out var d), id);
            Assert.AreSame(UsageDay.Utc, d, "an unknown id gives UTC, so a caller can fall back without a null");
        }
        Assert.AreSame(TimeZoneInfo.Utc, UsageDay.Utc.Zone);
        Assert.ThrowsException<ArgumentNullException>(() => new UsageDay(null!));
    }
}

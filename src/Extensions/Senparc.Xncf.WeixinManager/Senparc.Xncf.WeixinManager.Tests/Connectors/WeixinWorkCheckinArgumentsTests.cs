using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.Connectors;

namespace Senparc.Xncf.WeixinManager.Tests.Connectors;

[TestClass]
public class WeixinWorkCheckinArgumentsTests
{
    [TestMethod]
    public void MissingUserIdIsRejected()
    {
        var ok = WeixinWorkCheckinArguments.TryCreate(
            new Dictionary<string, string>(),
            3,
            out _,
            out var error);

        Assert.IsFalse(ok);
        Assert.IsTrue(error.Contains("userid", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SplitsMembersAndDefaultsToday()
    {
        var ok = WeixinWorkCheckinArguments.TryCreate(
            new Dictionary<string, string> { ["userIds"] = "zhangsan，lisi lisi" },
            3,
            out var query,
            out var error);

        Assert.IsTrue(ok, error);
        CollectionAssert.AreEqual(new[] { "zhangsan", "lisi" }, query.UserIds);
        Assert.AreEqual(DateTime.Today, query.Start.Date);
        Assert.AreEqual(3, query.CheckinType);
    }

    [TestMethod]
    public void RejectsRangeLongerThanThirtyDays()
    {
        var ok = WeixinWorkCheckinArguments.TryCreate(
            new Dictionary<string, string>
            {
                ["userIds"] = "zhangsan",
                ["startTime"] = "2026-01-01",
                ["endTime"] = "2026-03-01"
            },
            1,
            out _,
            out var error);

        Assert.IsFalse(ok);
        Assert.IsTrue(error.Contains("30"));
    }
}

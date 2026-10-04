using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;

namespace Senparc.Xncf.WeixinManager.Tests.WeixinClaw;

[TestClass]
public class WeixinClawHostedServiceOptionsTests
{
    [TestMethod]
    public void Getters_ClampOutOfRangeValues()
    {
        var options = new WeixinClawHostedServiceOptions
        {
            ScanIntervalSeconds = 0,
            ErrorRetryDelaySeconds = 999,
            ShutdownWaitSeconds = 0,
            NotifyStopTimeoutSeconds = 999
        };

        Assert.AreEqual(TimeSpan.FromSeconds(1), options.GetScanInterval());
        Assert.AreEqual(TimeSpan.FromSeconds(300), options.GetErrorRetryDelay());
        Assert.AreEqual(TimeSpan.FromSeconds(1), options.GetShutdownWait());
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.GetNotifyStopTimeout());
    }

    [TestMethod]
    public void Getters_ReturnConfiguredValuesWithinRange()
    {
        var options = new WeixinClawHostedServiceOptions
        {
            ScanIntervalSeconds = 7,
            ErrorRetryDelaySeconds = 9,
            ShutdownWaitSeconds = 11,
            NotifyStopTimeoutSeconds = 3
        };

        Assert.AreEqual(TimeSpan.FromSeconds(7), options.GetScanInterval());
        Assert.AreEqual(TimeSpan.FromSeconds(9), options.GetErrorRetryDelay());
        Assert.AreEqual(TimeSpan.FromSeconds(11), options.GetShutdownWait());
        Assert.AreEqual(TimeSpan.FromSeconds(3), options.GetNotifyStopTimeout());
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LoginSlideTrackValidatorTests.cs
    文件功能描述：LoginSlideTrackValidator（登录滑块行为/位置校验，纯 C# 自研）单元测试

    创建标识：Senparc - 20261107
    创建描述：替换 LoginBehaviorSlideValidatorTests（Lazy.SlideCaptcha.Core 版），
              覆盖：位置校验、点数下限、时长下限、瞬移拦截、Percent 漂移自洽校验

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class LoginSlideTrackValidatorTests
{
    private const int BgWidth = 300;
    private const int SliderWidth = 50;

    /// <summary>
    /// 构造一条滑块轨迹（显示宽 300px，滑块宽 50px）：
    /// targetRatio 为终点位置占全行程比例（0~1，全行程 = 背景宽 - 滑块宽），
    /// reportedPercent 为客户端上报的 Percent（默认与终点反推值一致），
    /// durationMs 总时长，points 采样点数，jumpAt (0~1) 在轨迹该比例处注入一次 +100px 瞬移
    /// </summary>
    private static LoginSlideTrack BuildTrack(double targetRatio, float reportedPercent = float.NaN,
        double durationMs = 600, int points = 30, double jumpAt = -1)
    {
        var range = BgWidth - SliderWidth;
        var targetX = range * targetRatio;
        var start = DateTime.UtcNow.AddMilliseconds(-durationMs);
        var tracks = new List<LoginSlideTrackPoint>();
        for (int i = 0; i <= points; i++)
        {
            var x = targetX * i / points;
            if (jumpAt >= 0 && i == (int)(points * jumpAt))
            {
                x += 100;
            }
            tracks.Add(new LoginSlideTrackPoint
            {
                X = (int)x,
                Y = 0,
                T = (int)(durationMs * i / points)
            });
        }
        var lastX = tracks[tracks.Count - 1].X;
        var impliedPercent = lastX * 100d / range;
        var reported = float.IsNaN(reportedPercent) ? (float)impliedPercent : (float)reportedPercent;
        return new LoginSlideTrack
        {
            BackgroundImageWidth = BgWidth,
            BackgroundImageHeight = 187,
            SliderImageWidth = SliderWidth,
            SliderImageHeight = 187,
            StartTime = start,
            EndTime = start.AddMilliseconds(durationMs),
            Percent = reported,
            Tracks = tracks
        };
    }

    [TestMethod]
    public void HumanLikeTrack_AtTargetPosition_Passes()
    {
        var track = BuildTrack(0.42);
        Assert.IsTrue(LoginSlideTrackValidator.Validate(track, 42f));
    }

    [TestMethod]
    public void WrongPosition_Fails()
    {
        var track = BuildTrack(0.42);
        Assert.IsFalse(LoginSlideTrackValidator.Validate(track, 70f));
    }

    [TestMethod]
    public void PositionWithinTolerance_Passes()
    {
        // 终点 41.5%，目标 42%，容差 4% 内应通过
        var track = BuildTrack(0.415);
        Assert.IsTrue(LoginSlideTrackValidator.Validate(track, 42f));
    }

    [TestMethod]
    public void TooFewPoints_Fails()
    {
        var track = BuildTrack(0.42, points: 5);
        Assert.IsFalse(LoginSlideTrackValidator.Validate(track, 42f));
    }

    [TestMethod]
    public void TooFast_Fails()
    {
        var track = BuildTrack(0.42, durationMs: 120);
        Assert.IsFalse(LoginSlideTrackValidator.Validate(track, 42f));
    }

    [TestMethod]
    public void TeleportJump_Fails()
    {
        var track = BuildTrack(0.42, jumpAt: 0.5);
        Assert.IsFalse(LoginSlideTrackValidator.Validate(track, 42f));
    }

    [TestMethod]
    public void ReportedPercentDriftFromTrack_Fails()
    {
        // 轨迹终点反推 42%，但客户端谎报 Percent=60%，自洽校验应拒绝
        var track = BuildTrack(0.42, reportedPercent: 60f);
        Assert.IsFalse(LoginSlideTrackValidator.Validate(track, 60f));
    }

    [TestMethod]
    public void NullOrEmptyTrack_Fails()
    {
        Assert.IsFalse(LoginSlideTrackValidator.Validate(null, 42f));
        Assert.IsFalse(LoginSlideTrackValidator.Validate(new LoginSlideTrack(), 42f));
    }
}

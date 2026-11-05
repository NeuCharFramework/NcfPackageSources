using Lazy.SlideCaptcha.Core;
using Lazy.SlideCaptcha.Core.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class LoginBehaviorSlideValidatorTests
{
    /// <summary>
    /// 构造一条滑块轨迹（显示宽 300px）：
    /// targetRatio 终点位置（0~1 背景宽度比例，Percent 由服务端按轨迹终点计算），
    /// durationMs 总时长，points 采样点数，jumpAt (0~1) 在轨迹该比例处注入一次 +100px 瞬移
    /// </summary>
    private static SlideTrack BuildTrack(double targetRatio, double durationMs = 600, int points = 30,
        double jumpAt = -1)
    {
        var bgWidth = 300;
        var sliderWidth = 50;
        var targetX = bgWidth * targetRatio;
        var start = DateTime.UtcNow.AddMilliseconds(-durationMs);
        var tracks = new List<Track>();
        for (int i = 0; i <= points; i++)
        {
            var x = targetX * i / points;
            if (jumpAt >= 0 && i == (int)(points * jumpAt))
            {
                x += 100;
            }
            tracks.Add(new Track
            {
                X = (int)x,
                Y = 0,
                T = (int)(durationMs * i / points)
            });
        }
        return new SlideTrack
        {
            BackgroundImageWidth = bgWidth,
            BackgroundImageHeight = 187,
            SliderImageWidth = sliderWidth,
            SliderImageHeight = 187,
            StartTime = start,
            EndTime = start.AddMilliseconds(durationMs),
            Tracks = tracks
        };
    }

    private static CaptchaValidateData Data(double percent, float tolerant = 0.04f)
    {
        return new CaptchaValidateData((float)percent, tolerant);
    }

    [TestMethod]
    public void HumanLikeTrack_AtTargetPosition_Passes()
    {
        Assert.IsTrue(new LoginBehaviorSlideValidator().Validate(BuildTrack(0.42), Data(0.42)));
    }

    [TestMethod]
    public void WrongPosition_Fails()
    {
        Assert.IsFalse(new LoginBehaviorSlideValidator().Validate(BuildTrack(0.42), Data(0.70)));
    }

    [TestMethod]
    public void TooFewPoints_Fails()
    {
        Assert.IsFalse(new LoginBehaviorSlideValidator().Validate(BuildTrack(0.42, points: 5), Data(0.42)));
    }

    [TestMethod]
    public void TooFast_Fails()
    {
        Assert.IsFalse(new LoginBehaviorSlideValidator().Validate(BuildTrack(0.42, durationMs: 120), Data(0.42)));
    }

    [TestMethod]
    public void TeleportJump_Fails()
    {
        Assert.IsFalse(new LoginBehaviorSlideValidator().Validate(BuildTrack(0.42, jumpAt: 0.5), Data(0.42)));
    }
}

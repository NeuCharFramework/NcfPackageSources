/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LoginSlideCaptchaSupport.cs
    文件功能描述：登录滑块验证码支持：程序化背景渲染（SkiaSharp）与保守型行为轨迹校验器

    创建标识：Senparc - 20261105
    创建描述：基于 Lazy.SlideCaptcha.Core（MIT License）：
              1. ProceduralSlideBackgroundHandler 以 SkiaSharp 实时绘制 552x344 背景，仓库无需内置图片资源；
              2. LoginBehaviorSlideValidator 在位置校验之上叠加保守行为校验（时长/点数/单步位移/Percent 一致性），
                 拒绝明显的脚本伪造轨迹，同时保持对真实用户的低误判率。

----------------------------------------------------------------*/

using Lazy.SlideCaptcha.Core;
using Lazy.SlideCaptcha.Core.Validator;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 程序化背景资源处理：登录滑块验证码背景由 SkiaSharp 实时绘制（随机渐变 + 几何块 + 噪点），
    /// 每张验证码背景都不同，防止攻击者预采集固定模板；尺寸固定 552x344（组件要求）。
    /// </summary>
    public class ProceduralSlideBackgroundHandler : Lazy.SlideCaptcha.Core.Resources.Handler.IResourceHandler
    {
        public const string TYPE = "procedural";
        public const int Width = 552;
        public const int Height = 344;

        public bool CanHandle(string handlerType)
        {
            return handlerType == TYPE;
        }

        public byte[] Handle(Lazy.SlideCaptcha.Core.Resources.Resource resource)
        {
            var rng = new Random();
            using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            using var paint = new SKPaint { IsAntialias = true };

            // 随机深色渐变底（保证滑块块与干扰元素可见）
            var colorA = new SKColor((byte)(24 + rng.Next(60)), (byte)(32 + rng.Next(70)), (byte)(54 + rng.Next(90)));
            var colorB = new SKColor((byte)(10 + rng.Next(40)), (byte)(14 + rng.Next(36)), (byte)(24 + rng.Next(50)));
            using (var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(Width, Height),
                new[] { colorA, colorB },
                new[] { 0f, 1f },
                SKShaderTileMode.Clamp))
            {
                paint.Shader = shader;
                canvas.DrawRect(0, 0, Width, Height, paint);
            }
            paint.Shader = null;

            // 几何色块（制造局部亮度差异，干扰模板匹配）
            for (int i = 0; i < 16; i++)
            {
                paint.Color = new SKColor(
                    (byte)(90 + rng.Next(140)), (byte)(100 + rng.Next(130)), (byte)(120 + rng.Next(120)), (byte)(24 + rng.Next(48)));
                if (rng.Next(2) == 0)
                {
                    float x = rng.Next(Width) - 60;
                    float y = rng.Next(Height) - 40;
                    canvas.DrawRect(x, y, 40 + rng.Next(90), 24 + rng.Next(70), paint);
                }
                else
                {
                    canvas.DrawCircle(rng.Next(Width), rng.Next(Height), 12 + rng.Next(40), paint);
                }
            }

            // 细噪点
            paint.Color = new SKColor(255, 255, 255, 18);
            for (int i = 0; i < 500; i++)
            {
                canvas.DrawRect(rng.Next(Width), rng.Next(Height), 1.6f, 1.6f, paint);
            }
            paint.Color = new SKColor(0, 0, 0, 22);
            for (int i = 0; i < 500; i++)
            {
                canvas.DrawRect(rng.Next(Width), rng.Next(Height), 1.6f, 1.6f, paint);
            }

            using var image = surface.Snapshot();
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 85);
            return encoded.ToArray();
        }
    }

    /// <summary>
    /// 登录滑块行为校验器（继承 BaseValidator，替换默认 SimpleValidator）：
    /// 1. 轨迹点数下限（脚本一次性位移伪造的轨迹通常只有 1~3 个点）；
    /// 2. 拖拽时长下限（真人完成一次拖拽一般 &gt; 200ms）；
    /// 3. 单步 X 位移上限（禁止“瞬移”式跳跃）；
    /// 4. 最终位置校验：服务端按轨迹终点计算的 Percent（0~1 背景宽度比例）与目标位置比较，容差由 CaptchaOptions.Tolerant 控制。
    /// 规则均为保守阈值，目标是拦截明显自动化重试，而非识别真人。
    /// </summary>
    public class LoginBehaviorSlideValidator : BaseValidator
    {
        private const double MinDurationMs = 200;
        private const int MinTrackPoints = 10;
        private const double MaxStepX = 45;

        public override bool ValidateCore(SlideTrack slideTrack, CaptchaValidateData captchaValidateData)
        {
            if (slideTrack == null || slideTrack.Tracks == null || slideTrack.Tracks.Count < MinTrackPoints)
            {
                return false;
            }

            var durationMs = (slideTrack.EndTime - slideTrack.StartTime).TotalMilliseconds;
            if (durationMs < MinDurationMs)
            {
                return false;
            }

            for (int i = 1; i < slideTrack.Tracks.Count; i++)
            {
                double stepX = Math.Abs(slideTrack.Tracks[i].X - slideTrack.Tracks[i - 1].X);
                if (stepX > MaxStepX)
                {
                    return false;
                }
            }

            // Percent 为服务端按轨迹终点计算的只读比例（0~1 背景宽度），与目标位置按同一口径比较
            return Math.Abs(slideTrack.Percent - captchaValidateData.Percent) <= captchaValidateData.Tolerant;
        }
    }
}

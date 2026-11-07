/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LoginSlideCaptchaService.cs
    文件功能描述：登录滑块验证码服务（纯 C# + SVG 自研实现，零第三方依赖）

    创建标识：Senparc - 20261107
    创建描述：以自研滑块验证码替换 Lazy.SlideCaptcha.Core（MIT）+ SkiaSharp 方案：
              1. 背景/滑块图为纯 C# 生成的 SVG（程序化渐变 + 几何色块 + 噪点 + 拼图缺口），仓库零图片资源；
              2. 答案（缺口位置 Percent）存 CO2NET 对象缓存，一次性、5 分钟时效；
              3. 校验 = 位置校验（轨迹终点反推 Percent，容差 4%）+ 保守行为校验
                 （时长≥200ms、点数≥10、单步位移≤45px、上报 Percent 与轨迹自洽）；
              4. 前端契约保持不变（id/backgroundImage/sliderImage + 轨迹 JSON），前端无需改动。

----------------------------------------------------------------*/

using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 登录滑块验证码（自研、无第三方依赖）：
    /// - 场景图由随机种子驱动（渐变 + 几何块 + 噪点），背景与滑块块由同一场景裁切，视觉一致；
    /// - 缺口为拼图块形状（上下凸台随机），背景上以半透明暗色 + 描边标出；
    /// - 校验逻辑见 <see cref="LoginSlideTrackValidator"/>，阈值均为保守值：拦截明显自动化，不误伤真人。
    /// </summary>
    public class LoginSlideCaptchaService
    {
        /// <summary>
        /// 场景画布尺寸（原生坐标，前端按显示宽度等比缩放）
        /// </summary>
        public const int CanvasWidth = 552;
        public const int CanvasHeight = 344;

        public const int PieceWidth = 60;
        public const int PieceHeight = 84;
        public const int BumpRadius = 13;
        // 凸台 Bezier 控制点深度 ≈ 4/3 * r（单段三次曲线近似半圆）
        private const int BumpDepth = 17;

        private const string CacheKeyPrefix = "LoginSlideCaptcha:";
        private static readonly TimeSpan CaptchaLifeTime = TimeSpan.FromMinutes(5);

        private readonly IBaseObjectCacheStrategy _cache =
            CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();

        /// <summary>
        /// 验证码生成结果（Token 即登录提交时回填的 id）
        /// </summary>
        public class CaptchaModel
        {
            public string Token { get; set; }
            public string BackgroundImage { get; set; }
            public string SliderImage { get; set; }
        }

        private sealed class SlideCaptchaState
        {
            public float TargetPercent { get; set; }
        }

        /// <summary>
        /// 生成新的滑块验证码（答案写入缓存，一次性有效）
        /// </summary>
        public async Task<CaptchaModel> CreateAsync()
        {
            byte[] seedBuffer = new byte[4];
            using (var rngProvider = RandomNumberGenerator.Create())
            {
                rngProvider.GetBytes(seedBuffer);
            }
            var rng = new Random(BitConverter.ToInt32(seedBuffer, 0));

            var scene = SceneBuilder.Generate(rng);
            int pieceX = 70 + rng.Next(CanvasWidth - PieceWidth - 140);
            int pieceY = 40 + rng.Next(CanvasHeight - PieceHeight - 80);
            bool bumpUp = rng.Next(2) == 0;

            float targetPercent = (float)pieceX / (CanvasWidth - PieceWidth) * 100f;
            var puzzle = PuzzlePath.Create(pieceX, pieceY, bumpUp);

            string token = Guid.NewGuid().ToString("N");
            await _cache.SetAsync(CacheKeyPrefix + token,
                new SlideCaptchaState { TargetPercent = targetPercent }, CaptchaLifeTime);

            return new CaptchaModel
            {
                Token = token,
                BackgroundImage = ToDataUri(SvgRenderer.BuildBackground(scene, puzzle.Absolute)),
                SliderImage = ToDataUri(SvgRenderer.BuildPiece(scene, puzzle.Local, pieceX))
            };
        }

        /// <summary>
        /// 校验滑块（一次性：无论成功失败都会使该 Token 失效）
        /// </summary>
        public async Task<bool> ValidateAsync(string token, LoginSlideTrack track)
        {
            if (string.IsNullOrWhiteSpace(token) || track == null)
            {
                return false;
            }

            string key = CacheKeyPrefix + token.Trim();
            var state = await _cache.GetAsync<SlideCaptchaState>(key);
            await _cache.RemoveFromCacheAsync(key);

            if (state == null)
            {
                return false;
            }
            return LoginSlideTrackValidator.Validate(track, state.TargetPercent);
        }

        private static string ToDataUri(string svg)
        {
            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        }

        #region 场景与拼图路径（内部模型）

        private struct SceneShape
        {
            public int Kind; // 0=rect 1=circle 2=ellipse
            public float X, Y, W, H;
            public int R;
            public int Red, Green, Blue, Alpha; // Alpha 0~255
        }

        private struct NoiseDot
        {
            public float X, Y, R;
            public bool Light;
        }

        private sealed class Scene
        {
            public int GradRed1, GradGreen1, GradBlue1;
            public int GradRed2, GradGreen2, GradBlue2;
            public List<SceneShape> Shapes { get; } = new List<SceneShape>();
            public List<NoiseDot> Noise { get; } = new List<NoiseDot>();
        }

        private static class SceneBuilder
        {
            public static Scene Generate(Random rng)
            {
                var scene = new Scene
                {
                    GradRed1 = 24 + rng.Next(60),
                    GradGreen1 = 32 + rng.Next(70),
                    GradBlue1 = 54 + rng.Next(90),
                    GradRed2 = 10 + rng.Next(40),
                    GradGreen2 = 14 + rng.Next(36),
                    GradBlue2 = 24 + rng.Next(50)
                };

                for (int i = 0; i < 16; i++)
                {
                    scene.Shapes.Add(new SceneShape
                    {
                        Kind = rng.Next(3),
                        X = rng.Next(CanvasWidth) - 60,
                        Y = rng.Next(CanvasHeight) - 40,
                        W = 40 + rng.Next(90),
                        H = 24 + rng.Next(70),
                        R = 12 + rng.Next(40),
                        Red = 90 + rng.Next(140),
                        Green = 100 + rng.Next(130),
                        Blue = 120 + rng.Next(120),
                        Alpha = 24 + rng.Next(48)
                    });
                }

                for (int i = 0; i < 120; i++)
                {
                    scene.Noise.Add(new NoiseDot
                    {
                        X = (float)rng.NextDouble() * CanvasWidth,
                        Y = (float)rng.NextDouble() * CanvasHeight,
                        R = 0.8f + (float)rng.NextDouble() * 1.4f,
                        Light = rng.Next(2) == 0
                    });
                }
                return scene;
            }
        }

        private sealed class PuzzlePath
        {
            public string Absolute { get; }
            public string Local { get; }

            private PuzzlePath(string absolute, string local)
            {
                Absolute = absolute;
                Local = local;
            }

            public static PuzzlePath Create(int x0, int y0, bool bumpUp)
            {
                int cx = x0 + PieceWidth / 2;
                int d = BumpDepth, r = BumpRadius, w = PieceWidth, h = PieceHeight;
                var abs = new StringBuilder();
                var local = new StringBuilder();
                Append(abs, x0, y0, cx, r, d, w, h, bumpUp);
                Append(local, 0, y0, PieceWidth / 2, r, d, w, h, bumpUp);
                return new PuzzlePath(abs.ToString(), local.ToString());
            }

            private static void Append(StringBuilder sb, int x, int y0, int cx, int r, int d, int w, int h, bool bumpUp)
            {
                if (bumpUp)
                {
                    sb.Append("M ").Append(x).Append(' ').Append(y0 + h)
                      .Append(" L ").Append(x).Append(' ').Append(y0)
                      .Append(" L ").Append(cx - r).Append(' ').Append(y0)
                      .Append(" C ").Append(cx - r).Append(' ').Append(y0 - d)
                      .Append(' ').Append(cx + r).Append(' ').Append(y0 - d)
                      .Append(' ').Append(cx + r).Append(' ').Append(y0)
                      .Append(" L ").Append(x + w).Append(' ').Append(y0)
                      .Append(" L ").Append(x + w).Append(' ').Append(y0 + h)
                      .Append(" Z");
                }
                else
                {
                    sb.Append("M ").Append(x).Append(' ').Append(y0)
                      .Append(" L ").Append(x + w).Append(' ').Append(y0)
                      .Append(" L ").Append(x + w).Append(' ').Append(y0 + h)
                      .Append(" L ").Append(cx + r).Append(' ').Append(y0 + h)
                      .Append(" C ").Append(cx + r).Append(' ').Append(y0 + h + d)
                      .Append(' ').Append(cx - r).Append(' ').Append(y0 + h + d)
                      .Append(' ').Append(cx - r).Append(' ').Append(y0 + h)
                      .Append(" L ").Append(x).Append(' ').Append(y0 + h)
                      .Append(" Z");
                }
            }
        }

        private static class SvgRenderer
        {
            public static string BuildBackground(Scene scene, string holePath)
            {
                var sb = new StringBuilder(8192);
                sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(CanvasWidth)
                  .Append("\" height=\"").Append(CanvasHeight)
                  .Append("\" viewBox=\"0 0 ").Append(CanvasWidth).Append(' ').Append(CanvasHeight).Append("\">");
                AppendDefs(sb, scene, holePath, "slide-hole");
                sb.Append("<rect width=\"").Append(CanvasWidth).Append("\" height=\"").Append(CanvasHeight)
                  .Append("\" fill=\"url(#slide-grad)\"/>");
                AppendScene(sb, scene, 0);
                // 缺口：半透明暗色覆盖 + 浅色描边
                sb.Append("<g clip-path=\"url(#slide-hole)\"><rect width=\"").Append(CanvasWidth)
                  .Append("\" height=\"").Append(CanvasHeight)
                  .Append("\" fill=\"rgba(0,0,0,0.45)\"/></g>");
                sb.Append("<path d=\"").Append(holePath)
                  .Append("\" fill=\"none\" stroke=\"rgba(255,255,255,0.4)\" stroke-width=\"1.5\"/>");
                sb.Append("</svg>");
                return sb.ToString();
            }

            public static string BuildPiece(Scene scene, string localPath, int pieceX)
            {
                var sb = new StringBuilder(8192);
                sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(PieceWidth)
                  .Append("\" height=\"").Append(CanvasHeight)
                  .Append("\" viewBox=\"0 0 ").Append(PieceWidth).Append(' ').Append(CanvasHeight).Append("\">");
                AppendDefs(sb, scene, localPath, "slide-piece");
                sb.Append("<g clip-path=\"url(#slide-piece)\">");
                sb.Append("<g transform=\"translate(").Append(-pieceX).Append(",0)\">");
                sb.Append("<rect width=\"").Append(CanvasWidth).Append("\" height=\"").Append(CanvasHeight)
                  .Append("\" fill=\"url(#slide-grad)\"/>");
                AppendScene(sb, scene, 0);
                sb.Append("</g>");
                sb.Append("<rect width=\"").Append(PieceWidth).Append("\" height=\"").Append(CanvasHeight)
                  .Append("\" fill=\"rgba(0,0,0,0.12)\"/>");
                sb.Append("</g>");
                sb.Append("<path d=\"").Append(localPath)
                  .Append("\" fill=\"none\" stroke=\"rgba(255,255,255,0.85)\" stroke-width=\"2\"/>");
                sb.Append("</svg>");
                return sb.ToString();
            }

            private static void AppendDefs(StringBuilder sb, Scene scene, string path, string clipId)
            {
                sb.Append("<defs>");
                sb.Append("<linearGradient id=\"slide-grad\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\">");
                sb.Append("<stop offset=\"0\" stop-color=\"rgb(")
                  .Append(scene.GradRed1).Append(',').Append(scene.GradGreen1).Append(',').Append(scene.GradBlue1).Append(")\"/>");
                sb.Append("<stop offset=\"1\" stop-color=\"rgb(")
                  .Append(scene.GradRed2).Append(',').Append(scene.GradGreen2).Append(',').Append(scene.GradBlue2).Append(")\"/>");
                sb.Append("</linearGradient>");
                sb.Append("<clipPath id=\"").Append(clipId).Append("\"><path d=\"").Append(path).Append("\"/></clipPath>");
                sb.Append("</defs>");
            }

            private static void AppendScene(StringBuilder sb, Scene scene, int xShift)
            {
                foreach (var shape in scene.Shapes)
                {
                    string fill = "rgba(" + shape.Red + ',' + shape.Green + ',' + shape.Blue + ','
                                   + Format(shape.Alpha / 255f) + ")";
                    switch (shape.Kind)
                    {
                        case 0:
                            sb.Append("<rect x=\"").Append(Format(shape.X + xShift)).Append("\" y=\"").Append(Format(shape.Y))
                              .Append("\" width=\"").Append(Format(shape.W)).Append("\" height=\"").Append(Format(shape.H))
                              .Append("\" fill=\"").Append(fill).Append("\"/>");
                            break;
                        case 1:
                            sb.Append("<circle cx=\"").Append(Format(shape.X + xShift + shape.R)).Append("\" cy=\"").Append(Format(shape.Y + shape.R))
                              .Append("\" r=\"").Append(shape.R).Append("\" fill=\"").Append(fill).Append("\"/>");
                            break;
                        default:
                            sb.Append("<ellipse cx=\"").Append(Format(shape.X + xShift + shape.W / 2)).Append("\" cy=\"").Append(Format(shape.Y + shape.H / 2))
                              .Append("\" rx=\"").Append(Format(shape.W / 2)).Append("\" ry=\"").Append(Format(shape.H / 2))
                              .Append("\" fill=\"").Append(fill).Append("\"/>");
                            break;
                    }
                }
                foreach (var dot in scene.Noise)
                {
                    sb.Append("<circle cx=\"").Append(Format(dot.X + xShift)).Append("\" cy=\"").Append(Format(dot.Y))
                      .Append("\" r=\"").Append(Format(dot.R)).Append("\" fill=\"")
                      .Append(dot.Light ? "rgba(255,255,255,0.10)" : "rgba(0,0,0,0.14)").Append("\"/>");
                }
            }

            private static string Format(float value)
            {
                return value.ToString("0.0", CultureInfo.InvariantCulture);
            }
        }

        #endregion
    }

    /// <summary>
    /// 客户端提交的滑块轨迹（字段名与前端 JSON 契约一致，大小写不敏感解析）
    /// </summary>
    public class LoginSlideTrack
    {
        public int BackgroundImageWidth { get; set; }
        public int BackgroundImageHeight { get; set; }
        public int SliderImageWidth { get; set; }
        public int SliderImageHeight { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public float Percent { get; set; }
        public List<LoginSlideTrackPoint> Tracks { get; set; }
    }

    public class LoginSlideTrackPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int T { get; set; }
    }

    /// <summary>
    /// 滑块轨迹校验器（纯函数，便于单元测试）：
    /// 1. 轨迹点数下限（脚本一次性位移伪造的轨迹通常只有 1~3 个点）；
    /// 2. 拖拽时长下限（真人完成一次拖拽一般 &gt; 200ms）；
    /// 3. 单步 X 位移上限（禁止“瞬移”式跳跃）；
    /// 4. 客户端上报 Percent 与轨迹终点反推值自洽（防止只伪造 Percent 字段）；
    /// 5. 位置校验：轨迹终点 Percent（0~100 全行程比例）与缺口目标位置比较，容差 4%。
    /// 注意：有效位置一律按轨迹终点反推，客户端上报值不可信。
    /// </summary>
    public static class LoginSlideTrackValidator
    {
        public const double MinDurationMs = 200;
        public const int MinTrackPoints = 10;
        public const double MaxStepX = 45;
        public const double PercentDriftTolerant = 2;
        public const double PositionTolerantPercent = 4;

        public static bool Validate(LoginSlideTrack track, float targetPercent)
        {
            if (track == null || track.Tracks == null || track.Tracks.Count < MinTrackPoints)
            {
                return false;
            }
            if (track.BackgroundImageWidth <= track.SliderImageWidth)
            {
                return false;
            }

            double durationMs = (track.EndTime - track.StartTime).TotalMilliseconds;
            if (durationMs < MinDurationMs)
            {
                return false;
            }

            for (int i = 1; i < track.Tracks.Count; i++)
            {
                double stepX = Math.Abs(track.Tracks[i].X - track.Tracks[i - 1].X);
                if (stepX > MaxStepX)
                {
                    return false;
                }
            }

            var last = track.Tracks[track.Tracks.Count - 1];
            int range = track.BackgroundImageWidth - track.SliderImageWidth;
            double impliedPercent = (double)last.X / range * 100d;
            if (Math.Abs(impliedPercent - track.Percent) > PercentDriftTolerant)
            {
                return false;
            }

            return Math.Abs(impliedPercent - targetPercent) <= PositionTolerantPercent;
        }
    }
}

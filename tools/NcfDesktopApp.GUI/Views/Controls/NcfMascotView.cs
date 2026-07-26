using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>无外部图片依赖、可缩放的 NCF Agent 矢量角色。</summary>
public sealed class NcfMascotView : Control
{
    public static readonly StyledProperty<NcfMascotKind> MascotProperty =
        AvaloniaProperty.Register<NcfMascotView, NcfMascotKind>(nameof(Mascot), NcfMascotKind.Nono);
    public static readonly StyledProperty<NcfMascotPose> PoseProperty =
        AvaloniaProperty.Register<NcfMascotView, NcfMascotPose>(nameof(Pose), NcfMascotPose.Idle);

    private readonly DispatcherTimer _timer;
    private double _phase;

    static NcfMascotView() => AffectsRender<NcfMascotView>(MascotProperty, PoseProperty);

    public NcfMascotView()
    {
        MinWidth = 32;
        MinHeight = 32;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _timer.Tick += (_, _) =>
        {
            _phase = (_phase + .16) % (Math.PI * 2);
            InvalidateVisual();
        };
    }

    public NcfMascotKind Mascot
    {
        get => GetValue(MascotProperty);
        set => SetValue(MascotProperty, value);
    }

    public NcfMascotPose Pose
    {
        get => GetValue(PoseProperty);
        set => SetValue(PoseProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var side = Math.Min(Bounds.Width, Bounds.Height);
        if (side < 1)
        {
            return;
        }

        var scale = side / 100d;
        var motion = GetMotion();
        var center = new Point(
            (Bounds.Width - side) / 2 + side / 2 + motion.X * scale,
            (Bounds.Height - side) / 2 + motion.Y * scale);
        var colors = MascotColors.For(Mascot);
        var outline = new Pen(colors.Outline, Math.Max(1.1, 1.65 * scale));

        DrawRoleBackground(context, center, scale, colors);
        context.DrawEllipse(Brush(34, 15, 23, 42), null,
            new Point(center.X, center.Y + 91 * scale),
            27 * scale * motion.Shadow, 5 * scale);
        DrawArms(context, center, scale, colors, motion);
        DrawBody(context, center, scale, colors, outline);
        DrawHead(context, center, scale, colors, outline, motion);
        DrawRoleDetail(context, center, scale, colors, outline, motion);
    }

    private Motion GetMotion()
    {
        var wave = Math.Sin(_phase * 2.2);
        var pulse = (Math.Sin(_phase) + 1) / 2;
        return Pose switch
        {
            NcfMascotPose.Wave => new(0, -1.3 + wave * .8, .98, -35 + wave * 24, 34, 0, pulse),
            NcfMascotPose.Thinking => new(0, -.8 + Math.Sin(_phase * .7) * .55, .99, -8, -72, 2.4, pulse),
            NcfMascotPose.Working => new(0, -1.8 + Math.Abs(wave) * 1.3, .96, -26 + wave * 22, 26 - wave * 22, 0, pulse),
            NcfMascotPose.Success => new(0, -2.8 - Math.Abs(wave) * 2.2, .92, -66, 66, 0, pulse),
            NcfMascotPose.Warning => new(wave * 1.7, -.4, 1.02, 18, -18, 0, pulse),
            _ => new(0, -1.2 + Math.Sin(_phase) * 1.1, .98 + pulse * .02, -12, 12, 0, pulse)
        };
    }

    private static void DrawArms(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors,
        Motion motion)
    {
        DrawArm(context, center, scale, colors, true, motion.LeftArm);
        DrawArm(context, center, scale, colors, false, motion.RightArm);
    }

    private static void DrawArm(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors,
        bool isLeft,
        double angle)
    {
        var direction = isLeft ? -1d : 1d;
        var shoulder = new Point(center.X + direction * 26 * scale, center.Y + 60 * scale);
        var radians = (90 + direction * angle) * Math.PI / 180d;
        var hand = new Point(
            shoulder.X + Math.Cos(radians) * 23 * scale,
            shoulder.Y + Math.Sin(radians) * 23 * scale);
        context.DrawLine(new Pen(colors.Outline, Math.Max(2.2, 5.2 * scale)), shoulder, hand);
        context.DrawEllipse(colors.Surface,
            new Pen(colors.Outline, Math.Max(1, 1.3 * scale)),
            hand, 5.6 * scale, 5.6 * scale);
    }

    private static void DrawBody(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors,
        Pen outline)
    {
        context.DrawEllipse(colors.Primary, outline,
            new Point(center.X, center.Y + 64 * scale), 29 * scale, 27 * scale);
        context.DrawEllipse(colors.Surface, null,
            new Point(center.X - 8 * scale, center.Y + 54 * scale), 9 * scale, 5 * scale);
        DrawDiamond(context, new Point(center.X, center.Y + 65 * scale),
            8.5 * scale, colors.Glow, colors.Highlight);

        var legs = new Pen(colors.Outline, Math.Max(2.3, 5.6 * scale));
        context.DrawLine(legs,
            new Point(center.X - 11 * scale, center.Y + 84 * scale),
            new Point(center.X - 13 * scale, center.Y + 91 * scale));
        context.DrawLine(legs,
            new Point(center.X + 11 * scale, center.Y + 84 * scale),
            new Point(center.X + 13 * scale, center.Y + 91 * scale));
    }

    private void DrawHead(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors,
        Pen outline,
        Motion motion)
    {
        var head = new Point(center.X, center.Y + 32 * scale);
        context.DrawEllipse(colors.Surface, outline, head, 34 * scale, 29 * scale);
        context.DrawEllipse(
            Brush(30, colors.PrimaryColor.R, colors.PrimaryColor.G, colors.PrimaryColor.B),
            null, new Point(head.X - 10 * scale, head.Y - 10 * scale), 15 * scale, 8 * scale);
        DrawFace(context, head, scale, colors, motion);
    }

    private void DrawFace(
        DrawingContext context,
        Point head,
        double scale,
        MascotColors colors,
        Motion motion)
    {
        var eyePen = new Pen(colors.Outline, Math.Max(1.5, 2.5 * scale));
        var blink = Pose is not (NcfMascotPose.Warning or NcfMascotPose.Success)
                    && Math.Sin(_phase * .5) > .985;
        var glance = Pose == NcfMascotPose.Thinking ? motion.Eye * scale : 0;

        if (Pose == NcfMascotPose.Success)
        {
            context.DrawLine(eyePen,
                new Point(head.X - 18 * scale, head.Y - scale),
                new Point(head.X - 10 * scale, head.Y + 3 * scale));
            context.DrawLine(eyePen,
                new Point(head.X + 10 * scale, head.Y + 3 * scale),
                new Point(head.X + 18 * scale, head.Y - scale));
        }
        else if (blink)
        {
            context.DrawLine(eyePen,
                new Point(head.X - 19 * scale, head.Y),
                new Point(head.X - 10 * scale, head.Y));
            context.DrawLine(eyePen,
                new Point(head.X + 10 * scale, head.Y),
                new Point(head.X + 19 * scale, head.Y));
        }
        else
        {
            context.DrawEllipse(colors.Outline, null,
                new Point(head.X - 14 * scale + glance, head.Y - scale), 3.7 * scale, 5.3 * scale);
            context.DrawEllipse(colors.Outline, null,
                new Point(head.X + 14 * scale + glance, head.Y - scale), 3.7 * scale, 5.3 * scale);
        }

        DrawExpression(context, head, scale, colors, eyePen);
    }

    private void DrawExpression(
        DrawingContext context,
        Point head,
        double scale,
        MascotColors colors,
        Pen eyePen)
    {
        if (Pose == NcfMascotPose.Warning)
        {
            context.DrawLine(eyePen,
                new Point(head.X - 20 * scale, head.Y - 11 * scale),
                new Point(head.X - 10 * scale, head.Y - 8 * scale));
            context.DrawLine(eyePen,
                new Point(head.X + 10 * scale, head.Y - 8 * scale),
                new Point(head.X + 20 * scale, head.Y - 11 * scale));
        }

        var mouth = Pose == NcfMascotPose.Success ? 9d : 6d;
        var mouthY = Pose == NcfMascotPose.Warning ? 10d : 13d;
        context.DrawLine(new Pen(colors.Outline, Math.Max(1.1, 1.7 * scale)),
            new Point(head.X - mouth * scale, head.Y + 13 * scale),
            new Point(head.X + mouth * scale, head.Y + mouthY * scale));
    }

    private void DrawRoleBackground(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors)
    {
        if (Mascot != NcfMascotKind.Qiao)
        {
            return;
        }

        var pulse = .82 + (Math.Sin(_phase) + 1) * .08;
        var ring = new Pen(
            Brush(95, colors.PrimaryColor.R, colors.PrimaryColor.G, colors.PrimaryColor.B),
            Math.Max(1, 1.6 * scale));
        context.DrawEllipse(null, ring, new Point(center.X, center.Y + 42 * scale),
            42 * scale * pulse, 39 * scale * pulse);
        context.DrawEllipse(null, ring, new Point(center.X, center.Y + 42 * scale),
            47 * scale * pulse, 44 * scale * pulse);
    }

    private void DrawRoleDetail(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors,
        Pen outline,
        Motion motion)
    {
        switch (Mascot)
        {
            case NcfMascotKind.Nono:
                context.DrawLine(new Pen(colors.Outline, Math.Max(1.2, 1.8 * scale)),
                    new Point(center.X, center.Y + 4 * scale),
                    new Point(center.X, center.Y - 4 * scale));
                DrawDiamond(context, new Point(center.X, center.Y - 8 * scale),
                    4.5 * scale, colors.Primary, colors.Highlight);
                break;
            case NcfMascotKind.Cici:
                DrawHeadset(context, center, scale, colors);
                break;
            case NcfMascotKind.Qiao:
                context.DrawEllipse(colors.Primary, outline,
                    new Point(center.X - 36 * scale, center.Y + 32 * scale), 5.5 * scale, 8 * scale);
                context.DrawEllipse(colors.Primary, outline,
                    new Point(center.X + 36 * scale, center.Y + 32 * scale), 5.5 * scale, 8 * scale);
                break;
            case NcfMascotKind.Opsi:
                context.DrawLine(new Pen(colors.Outline, Math.Max(2.2, 4.8 * scale)),
                    new Point(center.X - 23 * scale, center.Y + 75 * scale),
                    new Point(center.X + 23 * scale, center.Y + 75 * scale));
                context.DrawEllipse(motion.Pulse > .55 ? colors.Highlight : colors.Primary, null,
                    new Point(center.X + 20 * scale, center.Y + 75 * scale), 3 * scale, 3 * scale);
                break;
        }
    }

    private static void DrawHeadset(
        DrawingContext context,
        Point center,
        double scale,
        MascotColors colors)
    {
        var headset = new Pen(colors.Primary, Math.Max(2.1, 4.2 * scale));
        context.DrawLine(headset,
            new Point(center.X - 33 * scale, center.Y + 26 * scale),
            new Point(center.X - 35 * scale, center.Y + 43 * scale));
        context.DrawLine(headset,
            new Point(center.X + 33 * scale, center.Y + 26 * scale),
            new Point(center.X + 35 * scale, center.Y + 43 * scale));
        context.DrawLine(new Pen(colors.Primary, Math.Max(1.2, 2 * scale)),
            new Point(center.X + 35 * scale, center.Y + 42 * scale),
            new Point(center.X + 26 * scale, center.Y + 49 * scale));
        context.DrawEllipse(colors.Primary, null,
            new Point(center.X + 24 * scale, center.Y + 49 * scale), 2.5 * scale, 2.5 * scale);
    }

    private static void DrawDiamond(
        DrawingContext context,
        Point center,
        double radius,
        IBrush fill,
        IBrush stroke)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(center.X, center.Y - radius), true);
            geometryContext.LineTo(new Point(center.X + radius, center.Y));
            geometryContext.LineTo(new Point(center.X, center.Y + radius));
            geometryContext.LineTo(new Point(center.X - radius, center.Y));
            geometryContext.EndFigure(true);
        }

        context.DrawGeometry(fill, new Pen(stroke, Math.Max(1, radius * .17)), geometry);
    }

    private static SolidColorBrush Brush(byte alpha, byte red, byte green, byte blue) =>
        new(Color.FromArgb(alpha, red, green, blue));

    private readonly record struct Motion(
        double X,
        double Y,
        double Shadow,
        double LeftArm,
        double RightArm,
        double Eye,
        double Pulse);

    private readonly record struct MascotColors(
        Color PrimaryColor,
        IBrush Primary,
        IBrush Outline,
        IBrush Surface,
        IBrush Glow,
        IBrush Highlight)
    {
        public static MascotColors For(NcfMascotKind kind) => kind switch
        {
            NcfMascotKind.Cici => Create("#7C3AED", "#3B1B67", "#F7F3FF", "#C4B5FD"),
            NcfMascotKind.Qiao => Create("#06B6D4", "#164E63", "#ECFEFF", "#67E8F9"),
            NcfMascotKind.Opsi => Create("#F59E0B", "#713F12", "#FFFBEB", "#FDE68A"),
            _ => Create("#2563EB", "#172554", "#EFF6FF", "#93C5FD")
        };

        private static MascotColors Create(
            string primary,
            string outline,
            string surface,
            string highlight)
        {
            var primaryColor = Color.Parse(primary);
            var highlightColor = Color.Parse(highlight);
            return new MascotColors(
                primaryColor,
                new SolidColorBrush(primaryColor),
                new SolidColorBrush(Color.Parse(outline)),
                new SolidColorBrush(Color.Parse(surface)),
                Brush(210, highlightColor.R, highlightColor.G, highlightColor.B),
                new SolidColorBrush(highlightColor));
        }
    }
}

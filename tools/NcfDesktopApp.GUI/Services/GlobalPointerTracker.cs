using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// 读取桌面鼠标位置的轻量级适配器。平台 API 不可用时由调用方继续使用窗口内指针事件。
/// </summary>
internal static class GlobalPointerTracker
{
    private static IntPtr _x11Display;

    public static bool TryGetScreenPosition(out PixelPoint position)
    {
        if (OperatingSystem.IsWindows() && GetCursorPos(out var windowsPoint))
        {
            position = new PixelPoint(windowsPoint.X, windowsPoint.Y);
            return true;
        }

        if (OperatingSystem.IsMacOS() && TryGetMacOsPosition(out position))
        {
            return true;
        }

        if (OperatingSystem.IsLinux() && TryGetX11Position(out position))
        {
            return true;
        }

        position = default;
        return false;
    }

    private static bool TryGetMacOsPosition(out PixelPoint position)
    {
        position = default;
        IntPtr eventRef = IntPtr.Zero;
        try
        {
            eventRef = CGEventCreate(IntPtr.Zero);
            if (eventRef == IntPtr.Zero)
            {
                return false;
            }

            var point = CGEventGetLocation(eventRef);
            position = new PixelPoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            if (eventRef != IntPtr.Zero)
            {
                CFRelease(eventRef);
            }
        }
    }

    private static bool TryGetX11Position(out PixelPoint position)
    {
        position = default;
        try
        {
            _x11Display = _x11Display == IntPtr.Zero
                ? XOpenDisplay(IntPtr.Zero)
                : _x11Display;
            if (_x11Display == IntPtr.Zero)
            {
                return false;
            }

            var rootWindow = XDefaultRootWindow(_x11Display);
            return XQueryPointer(
                _x11Display,
                rootWindow,
                out _,
                out _,
                out var rootX,
                out var rootY,
                out _,
                out _,
                out _)
                && SetPosition(rootX, rootY, out position);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool SetPosition(int x, int y, out PixelPoint position)
    {
        position = new PixelPoint(x, y);
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsPoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CoreGraphicsPoint
    {
        public double X;
        public double Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out WindowsPoint point);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CoreGraphicsPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cfTypeRef);

    [DllImport("libX11")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11")]
    private static extern bool XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr rootWindow,
        out IntPtr childWindow,
        out int rootX,
        out int rootY,
        out int windowX,
        out int windowY,
        out uint mask);
}

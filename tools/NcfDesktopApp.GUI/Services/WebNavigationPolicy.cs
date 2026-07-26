using System;

namespace NcfDesktopApp.GUI.Services;

internal static class WebNavigationPolicy
{
    public static bool TryGetNavigableUri(string? value, out Uri uri)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out uri!) &&
               uri.Scheme is "http" or "https";
    }
}

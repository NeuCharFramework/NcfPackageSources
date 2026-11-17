/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: DesktopBridgeResource.cs
    File description: localized display metadata for XNCF registration
    
    Created by: Senparc - 20261111
    
    Modified by: Senparc - 20261111
    Change description: v0.4.10 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DesktopBridge;

public sealed class DesktopBridgeResource
{
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(DesktopBridgeResource), key, fallback);
}

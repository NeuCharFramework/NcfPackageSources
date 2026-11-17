/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ConnectorsResource.cs
    File description: localized display metadata for XNCF registration
    
    Created by: Senparc - 20261111
    
    Modified by: Senparc - 20261111
    Change description: v0.1.0 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Connectors;

public sealed class ConnectorsResource
{
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(ConnectorsResource), key, fallback);
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkspaceResource.cs
    File description: localized display metadata for XNCF registration
    
    Created by: Senparc - 20261111
    
    Modified by: Senparc - 20261111
    Change description: v0.2.7 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceResource
{
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(WorkspaceResource), key, fallback);

    public static string Format(string key, string fallback, params object[] arguments) =>
        ResourceStringLocalizer.Format(typeof(WorkspaceResource), key, fallback, arguments);
}

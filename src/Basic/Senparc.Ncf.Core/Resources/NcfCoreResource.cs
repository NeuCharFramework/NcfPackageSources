/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: NcfCoreResource.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260730
    
    Modified by: Senparc - 20260731
    Change description: v0.27.0-preview4 Updated module behavior and related support.

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.Core;

/// <summary>
/// Shared localization catalog for core validation messages.
/// </summary>
public sealed class NcfCoreResource
{
    public static string Get(string key, string fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(NcfCoreResource), key, fallback);
    }
}

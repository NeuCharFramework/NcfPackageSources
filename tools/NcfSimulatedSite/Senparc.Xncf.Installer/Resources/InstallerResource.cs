/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: InstallerResource.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260403
    
    Modified by: Senparc - 20260724
    Change description: v0.4.0 Updated module behavior and related support.

----------------------------------------------------------------*/
namespace Senparc.Xncf.Installer
{
    /// <summary>
    /// Marker class for Installer module localization resources.
    /// Resource files are stored in Resources/InstallerResource.{culture}.resx
    ///
    /// Usage in Razor views: @inject IStringLocalizer&lt;InstallerResource&gt; IR
    /// Usage in code:        IStringLocalizer&lt;InstallerResource&gt; localizer (via DI)
    ///
    /// Supported cultures: zh-CN (default), en, ja, fr, es, ru
    /// To add a new language: copy InstallerResource.en.resx, rename to InstallerResource.{culture}.resx,
    /// translate the values, and add the culture code to NcfLocalizationOptions.SupportedCultures.
    /// </summary>
    public class InstallerResource
    {
    }
}

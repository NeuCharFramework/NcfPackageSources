/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfRazorRuntimeCompilation.cs
    File description: IXncfRazorRuntimeCompilation related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Defines the optional source-path contract used by Razor runtime compilation.
    /// </summary>
    public interface IXncfRazorRuntimeCompilation
    {
        /// <summary>
        /// Gets the physical library path whose Razor views should be available for runtime compilation.
        /// </summary>
        string LibraryPath { get; }
    }
}

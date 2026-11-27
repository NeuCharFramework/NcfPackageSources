/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIgnoreMulitTenant.cs
    File description: IIgnoreMulitTenant related implementation

    Created by: Senparc - 20210218

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// Marks an entity or contract that must bypass ordinary tenant filtering.
    /// </summary>
    public interface IIgnoreMulitTenant
    {
    }
}

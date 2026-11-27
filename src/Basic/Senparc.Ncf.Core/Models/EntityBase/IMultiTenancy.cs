/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IMultiTenancy.cs
    File description: IMultiTenancy related implementation

    Created by: Senparc - 20201223

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

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// multi-tenancyinterface
    /// </summary>
    public interface IMultiTenancy
    {
        /// <summary>
        /// Gets or sets the tenant identifier owning the current entity or request.
        /// </summary>
        public int TenantId { get; set; }
    }
}

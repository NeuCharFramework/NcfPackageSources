/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ISoftDelete.cs
    File description: ISoftDelete related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Defines the soft-delete flag used by global entity filters.
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// Gets or sets whether the entity is excluded as deleted.
        /// </summary>
        bool Flag { get; set; }
    }
}

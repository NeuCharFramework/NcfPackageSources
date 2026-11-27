/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDtoBase.cs
    File description: IDtoBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Routing.Tree;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Defines common audit and soft-delete fields exposed by NCF DTOs.
    /// </summary>
    public interface IDtoBase<T> : IDtoBase
    {
        T Id { get; set; }
    }

    /// <summary>Defines common audit, tenant, and soft-delete fields for NCF DTOs.</summary>
    public interface IDtoBase
    {
        /// <summary>
        /// Gets or sets the soft-delete flag.
        /// </summary>
        bool Flag { get; set; }

        /// <summary>Gets or sets the administrator-only remark.</summary>
        [MaxLength(150)]
        string AdminRemark { get; set; }

        /// <summary>Gets or sets the general remark.</summary>
        [MaxLength(150)]
        string Remark { get; set; }

        /// <summary>Gets or sets the creation timestamp.</summary>
        DateTime AddTime { get; set; }
        /// <summary>Gets or sets the last-update timestamp.</summary>
        DateTime LastUpdateTime { get; set; }

        /// <summary>Gets or sets the tenant identifier.</summary>
        public int TenantId { get; set; }
    }
}

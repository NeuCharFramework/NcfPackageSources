/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DtoBase.cs
    File description: DtoBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Base DTO carrying a strongly typed value-type identifier.
    /// </summary>
    public class DtoBase<T> : DtoBase
        where T : struct
    {
        public T Id { get; set; }
    }

    /// <summary>
    /// Base DTO carrying an entity reference and strongly typed identifier.
    /// </summary>
    public class DtoBase<T, TID> : DtoBase
    where T : EntityBase<TID>
    {
        public TID Id { get; set; }
    }


    /// <summary>
    /// Base DTO carrying common audit, soft-delete, and tenant fields.
    /// </summary>
    public class DtoBase : IDtoBase
    {
        /// <summary>
        /// Gets or sets the soft-delete flag.
        /// </summary>
        public bool Flag { get; set; }

        /// <summary>
        /// Gets or sets an administrator-only remark.
        /// </summary>
        [MaxLength(150)]
        public string AdminRemark { get; set; }

        /// <summary>
        /// Gets or sets a general remark.
        /// </summary>
        [MaxLength(150)]
        public string Remark { get; set; }

        /// <summary>
        /// creation time
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// Gets or sets the most recent update time.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public int TenantId { get; set; }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntityBase.cs
    File description: EntityBase related implementation

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

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Provides common audit, soft-delete, tenant, and remark fields for NCF entities.
    /// </summary>
    [Serializable]
    public partial class EntityBase : IEntityBase, IMultiTenancy
    {
        #region Legacy section

        /// <summary>
        /// Gets or sets the soft-delete flag.
        /// </summary>
        public bool Flag { get; set; }
        /// <summary>
        /// creation time
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// Gets or sets the most recent update time.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        #endregion

        #region Legacy section

        /// <summary>
        /// Gets or sets the tenant identifier owning the entity.
        /// </summary>
        public int TenantId { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets an administrator-only remark.
        /// </summary>
        [MaxLength(300)]
        public string AdminRemark { get; set; }

        /// <summary>
        /// Gets or sets a general remark.
        /// </summary>
        [MaxLength(300)]
        public string Remark { get; set; }


        /// <summary>
        /// Initializes an entity with the current system time as its creation time.
        /// </summary>
        public EntityBase()
        {
            AddTime = SystemTime.Now.DateTime;
        }

        /// <summary>
        /// Updates <see cref="LastUpdateTime"/> and initializes <see cref="AddTime"/>
        /// when the entity has not yet been assigned a creation time.
        /// </summary>
        /// <param name="time">An explicit update time, or <see langword="null"/> for the current local time.</param>
        protected void SetUpdateTime(DateTime? time = null)
        {
            if (AddTime == DateTime.MinValue)
            {
                AddTime = SystemTime.Now.LocalDateTime;//Legacy comment translated to English.
            }
            LastUpdateTime = time ?? SystemTime.Now.LocalDateTime;
        }

    }

    /// <summary>
    /// Provides a strongly typed identifier in addition to the common entity fields.
    /// </summary>
    /// <typeparam name="TKey">The entity identifier type.</typeparam>
    [Serializable]
    public partial class EntityBase<TKey> : EntityBase, IEntityBase<TKey>, IMultiTenancyEntityBase<TKey>/*defaultsupportmulti-tenancyinterface*/
    {
        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        [Key]
        public TKey Id { get; set; }

    }
}

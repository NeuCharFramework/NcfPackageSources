/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntityBase.cs
    File description: EntityBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public partial class EntityBase : IEntityBase, IMultiTenancy
    {
        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Flag { get; set; }
        /// <summary>
        /// creation time
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        #endregion

        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public int TenantId { get; set; }

        #endregion

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(300)]
        public string AdminRemark { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(300)]
        public string Remark { get; set; }


        public EntityBase()
        {
            AddTime = SystemTime.Now.DateTime;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="time"></param>
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
    ///Legacy comment translated to English.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    [Serializable]
    public partial class EntityBase<TKey> : EntityBase, IEntityBase<TKey>, IMultiTenancyEntityBase<TKey>/*defaultsupportmulti-tenancyinterface*/
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Key]
        public TKey Id { get; set; }

    }
}

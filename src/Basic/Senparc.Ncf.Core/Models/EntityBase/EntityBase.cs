using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Database entity base class
    /// </summary>
    [Serializable]
    public partial class EntityBase : IEntityBase, IMultiTenancy
    {
        #region IEntityBase 接口

        /// <summary>
        /// Whether to soft delete
        /// </summary>
        public bool Flag { get; set; }
        /// <summary>
        ///add time
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// last updated time
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        #endregion

        #region IMultiTenancy 接口

        /// <summary>
        ///Tenant ID
        /// <para>If it is -1, multi-tenancy is not enabled in this system</para>
        /// <para>If it is 0, it is system public data (used in special circumstances)</para>
        /// </summary>
        public int TenantId { get; set; }

        #endregion

        /// <summary>
        ///Admin comments only
        /// </summary>
        [MaxLength(300)]
        public string AdminRemark { get; set; }

        /// <summary>
        /// Remarks visible to front-end users
        /// </summary>
        [MaxLength(300)]
        public string Remark { get; set; }


        public EntityBase()
        {
            AddTime = SystemTime.Now.DateTime;
        }

        /// <summary>
        ///Update last update time
        /// </summary>
        /// <param name="time"></param>
        protected void SetUpdateTime(DateTime? time = null)
        {
            if (AddTime == DateTime.MinValue)
            {
                AddTime = SystemTime.Now.LocalDateTime;//Usually happens when adding
            }
            LastUpdateTime = time ?? SystemTime.Now.LocalDateTime;
        }

    }

    /// <summary>
    /// Database entity base class with single primary key
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    [Serializable]
    public partial class EntityBase<TKey> : EntityBase, IEntityBase<TKey>, IMultiTenancyEntityBase<TKey>/*Supports multi-tenant interface by default*/
    {
        /// <summary>
        /// primary key
        /// </summary>
        [Key]
        public TKey Id { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 数据库实体基类
    /// </summary>
    [Serializable]
    public partial class EntityBase : IEntityBase, IMultiTenancy
    {
        #region IEntityBase 接口

        /// <summary>
        /// 是否软删除
        /// </summary>
        public bool Flag { get; set; }
        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddTime { get; set; }
        /// <summary>
        /// 上次更新时间
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        #endregion

        #region IMultiTenancy 接口

        /// <summary>
        /// 租户 ID
        /// <para>如果为-1，则本系统不启用多租户</para>
        /// <para>如果为0，则为系统公共数据（特殊情况使用）</para>
        /// </summary>
        public int TenantId { get; set; }

        #endregion

        /// <summary>
        /// 仅管理员备注
        /// </summary>
        [MaxLength(300)]
        public string AdminRemark { get; set; }

        /// <summary>
        /// 前台用户可见备注
        /// </summary>
        [MaxLength(300)]
        public string Remark { get; set; }


        public EntityBase()
        {
            AddTime = SystemTime.Now.DateTime;
        }

        /// <summary>
        /// 更新最后更新时间
        /// </summary>
        /// <param name="time"></param>
        protected void SetUpdateTime(DateTime? time = null)
        {
            if (AddTime == DateTime.MinValue)
            {
                AddTime = SystemTime.Now.LocalDateTime;//通常在添加的时候发生
            }
            LastUpdateTime = time ?? SystemTime.Now.LocalDateTime;
        }

    }

    /// <summary>
    /// 带单一主键的数据库实体基类
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    [Serializable]
    public partial class EntityBase<TKey> : EntityBase, IEntityBase<TKey>, IMultiTenancyEntityBase<TKey>/*默认支持多租户接口*/
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public TKey Id { get; set; }

    }
}

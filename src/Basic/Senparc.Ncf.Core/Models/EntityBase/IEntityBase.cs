using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 所有实体的最底层接口（支持软删除）
    /// </summary>
    public interface IEntityBase : ISoftDelete
    {
        /// <summary>
        /// 添加时间
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        DateTime LastUpdateTime { get; set; }
    }

    public interface IEntityBase<TKey> : IEntityBase
    {
        /// <summary>
        /// 主键
        /// </summary>
        TKey Id { get; set; }
    }

    public interface IMultiTenancyEntityBase<TKey> : IEntityBase<TKey>, IMultiTenancy
    {

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
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
}

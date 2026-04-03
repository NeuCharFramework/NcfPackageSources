using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// The lowest level interface of all entities (supports soft deletion)
    /// </summary>
    public interface IEntityBase : ISoftDelete
    {
        /// <summary>
        ///add time
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        ///Update time
        /// </summary>
        DateTime LastUpdateTime { get; set; }
    }

    public interface IEntityBase<TKey> : IEntityBase
    {
        /// <summary>
        /// primary key
        /// </summary>
        TKey Id { get; set; }
    }

    public interface IMultiTenancyEntityBase<TKey> : IEntityBase<TKey>, IMultiTenancy
    {

    }
}

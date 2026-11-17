/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IEntityBase.cs

    File description: IEntityBase related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IEntityBase : ISoftDelete
    {
        /// <summary>
        /// creation time
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        DateTime LastUpdateTime { get; set; }
    }

    public interface IEntityBase<TKey> : IEntityBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        TKey Id { get; set; }
    }

    public interface IMultiTenancyEntityBase<TKey> : IEntityBase<TKey>, IMultiTenancy
    {

    }
}

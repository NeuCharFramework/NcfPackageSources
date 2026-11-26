/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IEntityBase.cs
    File description: IEntityBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Defines the common persistence fields shared by all NCF entities.
    /// </summary>
    public interface IEntityBase : ISoftDelete
    {
        /// <summary>
        /// creation time
        /// </summary>
        DateTime AddTime { get; set; }
        /// <summary>
        /// Gets or sets the most recent update time.
        /// </summary>
        DateTime LastUpdateTime { get; set; }
    }

    public interface IEntityBase<TKey> : IEntityBase
    {
        /// <summary>
        /// Gets or sets the entity identifier.
        /// </summary>
        TKey Id { get; set; }
    }

    public interface IMultiTenancyEntityBase<TKey> : IEntityBase<TKey>, IMultiTenancy
    {

    }
}

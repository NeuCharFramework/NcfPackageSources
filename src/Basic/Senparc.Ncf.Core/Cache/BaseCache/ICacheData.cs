/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ICacheData.cs
    File description: ICacheData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>Defines the identity and timestamp fields required by cache data objects.</summary>
    public interface ICacheData
    {
        /// <summary>Gets the stable cache key.</summary>
        string Key { get; }

        /// <summary>Gets or sets the timestamp of the current cached value.</summary>
        DateTime CacheTime { get; set; }
    }
}

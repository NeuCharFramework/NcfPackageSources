/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ICacheData.cs
    File description: ICacheData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    public interface ICacheData
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Key { get; }

        DateTime CacheTime { get; set; }
    }
}

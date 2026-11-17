/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ICacheData.cs

    File description: ICacheData related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

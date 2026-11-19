/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: QueueCacheData.cs
    File description: QueueCacheData related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    [Serializable]
    public class QueueCacheData<T>
    {
        public string Key { get; set; }

        public DateTime LastActiveTime { get; set; }

        public T Data { get; set; }

        public QueueCacheData(string key)
        {
            Key = key;
            LastActiveTime = DateTime.Now;
        }
    }
}
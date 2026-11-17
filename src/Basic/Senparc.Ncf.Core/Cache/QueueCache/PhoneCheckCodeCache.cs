/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PhoneCheckCodeCache.cs
    File description: PhoneCheckCodeCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    [Serializable]
    public class PhoneCheckCodeData
    {
        public string Phone { get; set; }

        public PhoneCheckCodeData(string phone)
        {
            Phone = phone;
        }
    }

    ///// <summary>
    ///Legacy comment translated to English.
    ///// </summary>
    //public interface IPhoneCheckCodeCache : IQueueCache<PhoneCheckCodeData>
    //{

    //}

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public class PhoneCheckCodeCache : QueueCache<PhoneCheckCodeData>/*, IPhoneCheckCodeCache*/
    {
        private const string cacheKey = "PhoneCheckCodeCache";
        private const int timeoutSeconds = 10 * 60;
        public PhoneCheckCodeCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        public override string CreateKey()
        {
            string key = new Random().Next(100000,999999).ToString("######");
            return key;
        }

        public override QueueCacheData<PhoneCheckCodeData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);//Legacy comment translated to English.
            }
            return value;
        }
    }
}

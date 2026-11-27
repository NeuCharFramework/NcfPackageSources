/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PhoneCheckCodeCache.cs
    File description: PhoneCheckCodeCache related implementation

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
    /// <summary>
    /// Represents the phone number associated with a verification-code queue entry.
    /// </summary>
    [Serializable]
    public class PhoneCheckCodeData
    {
        public string Phone { get; set; }

        /// <summary>
        /// Initializes a phone verification-code payload.
        /// </summary>
        /// <param name="phone">The phone number for which the code was issued.</param>
        public PhoneCheckCodeData(string phone)
        {
            Phone = phone;
        }
    }

    ///// <summary>
    /// Stores phone verification-code payloads with a ten-minute expiration window.
    ///// </summary>
    //public interface IPhoneCheckCodeCache : IQueueCache<PhoneCheckCodeData>
    //{

    //}

    /// <summary>
    /// Stores phone verification-code payloads with a ten-minute expiration window.
    /// </summary>
    [Serializable]
    public class PhoneCheckCodeCache : QueueCache<PhoneCheckCodeData>/*, IPhoneCheckCodeCache*/
    {
        private const string cacheKey = "PhoneCheckCodeCache";
        private const int timeoutSeconds = 10 * 60;
        /// <summary>
        /// Initializes the phone verification-code cache.
        /// </summary>
        public PhoneCheckCodeCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        /// <summary>
        /// Generates a six-digit key suitable for a phone verification code.
        /// </summary>
        /// <returns>A six-digit numeric key.</returns>
        public override string CreateKey()
        {
            string key = new Random().Next(100000,999999).ToString("######");
            return key;
        }

        /// <summary>
        /// Gets a verification-code entry and removes it immediately when found, making the
        /// code single-use for callers that use this cache's default behavior.
        /// </summary>
        /// <param name="key">The six-digit verification-code key.</param>
        /// <param name="removeDataWhenExist">Whether expired entries should be removed during lookup.</param>
        /// <returns>The matching entry, or <see langword="null"/> when the key is absent.</returns>
        public override QueueCacheData<PhoneCheckCodeData> Get(string key, bool removeDataWhenExist = true)
        {
            var value = base.Get(key, removeDataWhenExist);
            if (value != null)
            {
                base.Remove(key);
            }
            return value;
        }
    }
}

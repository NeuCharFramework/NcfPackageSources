/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OAuthCodeCache.cs
    File description: OAuthCodeCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    /// <summary>
    /// Contains the account and application credentials associated with an OAuth code.
    /// </summary>
    [Serializable]
    public class OAuthCodeData
    {

        public int AccountId { get; set; }
        public int AppId { get; set; }
        public string AppKey { get; set; }
        public string AppSecret { get; set; }

        /// <summary>
        /// Initializes an OAuth code payload.
        /// </summary>
        /// <param name="accountId">The account identifier associated with the code.</param>
        /// <param name="appId">The application identifier associated with the code.</param>
        /// <param name="appKey">The application key used by the OAuth client.</param>
        /// <param name="appSecret">The application secret used by the OAuth client.</param>
        public OAuthCodeData(int accountId, int appId, string appKey, string appSecret)
        {
            AccountId = accountId;
            AppId = appId;
            AppKey = appKey;
            AppSecret = appSecret;
        }
    }

    /// <summary>
    /// Defines the queue-cache contract for OAuth code payloads.
    /// </summary>
    public interface IOAuthCodeCache : IQueueCache<OAuthCodeData>
    {

    }

    [Serializable]
    public class OAuthCodeCache : QueueCache<OAuthCodeData>, IOAuthCodeCache
    {
        private const string cacheKey = "OAuthCodeCache";
        private const int timeoutSeconds = 5 * 60;
        /// <summary>
        /// Initializes the OAuth code cache with its five-minute expiration window.
        /// </summary>
        public OAuthCodeCache()
            : base(cacheKey, timeoutSeconds)
        {

        }

        /// <summary>
        /// Generates a globally unique key for an OAuth code entry.
        /// </summary>
        /// <returns>A compact GUID-based key without separators.</returns>
        public override string CreateKey()
        {
            return Guid.NewGuid().ToString("n");
        }

        /// <summary>
        /// Gets an OAuth code entry and removes it immediately when found, enforcing
        /// one-time consumption of the code.
        /// </summary>
        /// <param name="key">The key assigned to the OAuth code.</param>
        /// <param name="removeDataWhenExist">Whether expired entries should be removed during lookup.</param>
        /// <returns>The matching entry, or <see langword="null"/> when the key is absent.</returns>
        public override QueueCacheData<OAuthCodeData> Get(string key, bool removeDataWhenExist = true)
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

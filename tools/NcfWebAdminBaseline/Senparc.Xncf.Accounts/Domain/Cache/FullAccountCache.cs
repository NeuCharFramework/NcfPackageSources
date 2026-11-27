/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FullAccountCache.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260724
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.3.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Xncf.Accounts.Domain.Cache
{
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Senparc.CO2NET;
    using Senparc.CO2NET.Extensions;
    using Senparc.Xncf.Accounts.Domain.Cache;
    using Senparc.Ncf.Core.DI;
    using Senparc.Ncf.Core.Enums;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Senparc.Ncf.Core.Models;
    using Senparc.Ncf.Core.Cache;

    [AutoDIType(DILifecycleType.Scoped)]
    /// <summary>Provides cached full-account projections keyed by user name.</summary>
    public class FullAccountCache : BaseStringDictionaryCache<FullAccount, Account> //, IFullAccountCache
    {
        /// <summary>Maps account identifiers to user names for cache lookups.</summary>
        public static ConcurrentDictionary<int, string> UserIdNameRelationshop = new ConcurrentDictionary<int, string>();

        public const string CACHE_KEY = "FullAccountCache";
        private const int timeout = 1440;

        private INcfDbData _dataContext => base._db as INcfDbData;

        #region Legacy section

        /// <summary>Loads the first account matching the supplied database predicate.</summary>
        /// <param name="where">The account predicate.</param>
        /// <returns>The matching account, or null.</returns>
        private Account GetAccount(Expression<Func<Account, bool>> where)
        {
            var account = (_dataContext.BaseDataContext as SenparcEntitiesBase).Set<Account>()
                .FirstOrDefault(where);
            return account;
        }

        public FullAccountCache(INcfDbData db)
            : base(CACHE_KEY, db, timeout)
        {
        }

        public override FullAccount Update()
        {
            return null;
        }

        public override FullAccount InsertObjectToCache(string key)
        {
            var account = this.GetAccount(z => z.UserName.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fullUser = this.InsertObjectToCache(key, account);
            return fullUser;
        }

        public override FullAccount GetObject(string key)
        {
            return base.GetObject(key);
        }

        public override FullAccount InsertObjectToCache(string key, Account obj)
        {
            var fullUser = base.InsertObjectToCache(key, obj);
            if (fullUser == null)
            {
                return null;
            }
            UserIdNameRelationshop[fullUser.Id] = fullUser.UserName;//Legacy comment translated to English.

            this.UpdateToCache(key, fullUser); //Legacy comment translated to English.
            return fullUser;
        }

        /// <summary>
        /// Marks a cached account as forcibly logged out and resets its activity time.
        /// Missing or blank user names are ignored.
        /// </summary>
        /// <param name="userName">The cached account user name.</param>
        public void ForceLogout(string userName)
        {
            if (userName.IsNullOrEmpty())
            {
                return;
            }
            var fullUser = GetObject(userName);
            if (fullUser == null)
            {
                return;
            }
            fullUser.ForceLogout = true;
            fullUser.LastActiveTime = DateTime.MinValue;
        }

        /// <summary>
        /// Resolves the cached user name associated with an account identifier.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <returns>The cached user name, or <see langword="null"/> when no relation exists.</returns>
        public string GetUserName(int accountId)
        {
            var userName = UserIdNameRelationshop.ContainsKey(accountId)
                ? UserIdNameRelationshop[accountId]
                : null;
            return userName;
        }
        /// <summary>
        /// Retrieves a full-account projection by identifier, loading the backing
        /// account into the cache when no identifier relation is known.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <returns>The cached projection, or <see langword="null"/> when no account exists.</returns>
        public FullAccount GetFullAccount(int accountId)
        {
            var userName = GetUserName(accountId);
            if (userName == null)
            {
                //Legacy comment translated to English.
                var account = this.GetAccount(z => z.Id == accountId);
                if (account == null)
                {
                    return null;
                }
                var fullUser = this.InsertObjectToCache(account.UserName, account);
                return fullUser;
            }
            return GetObject(userName);
        }

        public override void RemoveCache()
        {
            throw new Exception("不允许调用此方法！");
        }


        #endregion


        #region Legacy section


        /// <summary>
        /// Queries the underlying account table for one account matching the supplied predicate.
        /// This helper bypasses the normal single-value cache removal API.
        /// </summary>
        /// <param name="where">The predicate used to select the account.</param>
        /// <returns>The first matching account, or <see langword="null"/> when none exists.</returns>
        private async Task<Account> GetAccountAsync(Expression<Func<Account, bool>> where)
        {
            var account = await (_dataContext.BaseDataContext as SenparcEntitiesBase).Set<Account>()
                            .FirstOrDefaultAsync(where).ConfigureAwait(false);
            return account;
        }


        public override async Task<FullAccount> InsertObjectToCacheAsync(string key, Account obj)
        {
            var fullUser = await base.InsertObjectToCacheAsync(key, obj);
            if (fullUser == null)
            {
                return null;
            }
            UserIdNameRelationshop[fullUser.Id] = fullUser.UserName;//Legacy comment translated to English.

            await this.UpdateToCacheAsync(key, fullUser).ConfigureAwait(false); //Legacy comment translated to English.
            return fullUser;
        }

        public override async Task<FullAccount> InsertObjectToCacheAsync(string key)
        {
            var account = await this.GetAccountAsync(z => z.UserName.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fullUser = await this.InsertObjectToCacheAsync(key, account);
            return fullUser;
        }
        #endregion
    }
}

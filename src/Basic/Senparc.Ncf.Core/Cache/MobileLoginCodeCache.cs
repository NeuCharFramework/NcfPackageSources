/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MobileLoginCodeCache.cs
    File description: MobileLoginCodeCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using System;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache
{
    [Serializable]
    public class MobileLoginCode
    {
        /// <summary>Gets or sets the full user/domain name associated with the code.</summary>
        public string FullDomainUserName { get; set; }
        /// <summary>
        /// user name
        /// </summary>
        public string UserName
        {
            get
            {
                if (FullDomainUserName.IsNullOrEmpty())
                {
                    return string.Empty;
                }
                return FullDomainUserName.Split('@')[0];
            }
        }

        /// <summary>Gets the domain portion of <see cref="FullDomainUserName"/>.</summary>
        public string Domain
        {
            get
            {
                if (FullDomainUserName.IsNullOrEmpty())
                {
                    return string.Empty;
                }
                return FullDomainUserName.Split('@')[1];
            }
        }
        /// <summary>Gets or sets the plain verification code.</summary>
        public string Code { get; set; }
        /// <summary>Gets or sets the creation timestamp.</summary>
        public DateTime AddTime { get; set; }
        /// <summary>Gets or sets the URL associated with the login request.</summary>
        public string Url { get; set; }
        /// <summary>Gets a value indicating whether the code is within its three-minute validity window.</summary>
        public bool IsValid
        {
            get { return AddTime.AddMinutes(3) > DateTime.Now; }
        }
    }

    public interface IMobileLoginCodeCache : IBaseStringDictionaryCache<MobileLoginCode>
    {

    }

    [AutoDIType(DILifecycleType.Singleton)]
    public class MobileLoginCodeCache : BaseStringDictionaryCache<MobileLoginCode>, IMobileLoginCodeCache
    {
        /// <summary>
        /// Initializes the singleton mobile-login-code cache with a two-hour cache timeout.
        /// </summary>
        public MobileLoginCodeCache()
            : base("MobileLoginCodeCache", null, 120)
        {
        }

        /// <inheritdoc />
        public override MobileLoginCode InsertObjectToCache(string key)
        {
            var mobileLoginCode = new MobileLoginCode()
            {
                AddTime = DateTime.Now,
                FullDomainUserName = "",
                Code = key
            };
            return this.InsertObjectToCache(key, mobileLoginCode);
        }

        /// <inheritdoc />
        public override async Task<MobileLoginCode> InsertObjectToCacheAsync(string key)
        {
            var mobileLoginCode = new MobileLoginCode()
            {
                AddTime = DateTime.Now,
                FullDomainUserName = "",
                Code = key
            };
            return await this.InsertObjectToCacheAsync(key, mobileLoginCode).ConfigureAwait(false);
        }
    }
}

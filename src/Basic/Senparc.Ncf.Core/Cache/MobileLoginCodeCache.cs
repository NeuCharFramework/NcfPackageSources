/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MobileLoginCodeCache.cs
    File description: MobileLoginCodeCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
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
        public string Code { get; set; }
        public DateTime AddTime { get; set; }
        public string Url { get; set; }
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
        public MobileLoginCodeCache()
            : base("MobileLoginCodeCache", null, 120)
        {
        }

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

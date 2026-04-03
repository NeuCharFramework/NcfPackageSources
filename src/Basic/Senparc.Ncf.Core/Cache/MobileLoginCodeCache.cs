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
        /// Complete username with domain name format: <username>@<domain name>
        /// </summary>
        public string FullDomainUserName { get; set; }
        /// <summary>
        /// username
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
        ///domain name
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

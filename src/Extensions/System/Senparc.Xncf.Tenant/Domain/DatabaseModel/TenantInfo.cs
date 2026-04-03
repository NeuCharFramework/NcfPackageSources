using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.Cache;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.DataBaseModel
{
    /// <summary>
    ///tenant information
    /// </summary>
    [Table("TenantInfos")]
    public class TenantInfo : EntityBase<int>, IIgnoreMulitTenant //TODO: You can directly inherit the base class without using multi-tenancy
    {
        /// <summary>
        /// Globally unique number (automatically assigned)
        /// </summary>
        [Required]
        public Guid Guid { get; private set; }
        /// <summary>
        ///name
        /// </summary>
        [Required]
        public string Name { get; private set; }
        /// <summary>
        /// Whether to enable
        /// </summary>
        [Required]
        [DefaultValue(true)]
        public bool Enable { get; private set; }
        /// <summary>
        /// Match parameters of domain name, URL, and Head
        /// </summary>
        [Required]
        public string TenantKey { get; private set; }

        /// <summary>
        /// This attribute has been unmapped from the database
        /// </summary>
        [NotMapped]
        new public string TenantId { get; private set; }

        public TenantInfo(string name, bool enable, string tenantKey)
        {
            Guid = GenerateGuid();
            Name = name;
            Enable = enable;
            TenantKey = tenantKey;
        }

        private Guid GenerateGuid()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// update data
        /// </summary>
        /// <param name="dto"></param>
        public void Update(CreateOrUpdate_TenantInfoDto dto)
        {
            Name = dto.Name;
            Enable = dto.Enable;
            TenantKey = dto.TenantKey;
            AdminRemark = dto.AdminRemark;
        }

        /// <summary>
        /// clear cache
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public async Task ClearCache(IServiceProvider serviceProvider)
        {
            var fullTenantInfoCache = serviceProvider.GetRequiredService<FullTenantInfoCache>();
            await fullTenantInfoCache.RemoveCacheAsync();
        }
    }
}

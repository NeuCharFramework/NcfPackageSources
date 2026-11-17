/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: TenantInfo.cs
    File description: TenantInfo related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261005
    Change description: v0.15.11 0.15.11 Merge remote-tracking branch 'origin/Developer-MAF-V3' into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.15.14 Updated module behavior and related support.

----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    /// </summary>
    [Table("TenantInfos")]
    public class TenantInfo : EntityBase<int>, IIgnoreMulitTenant //Legacy comment translated to English.
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public Guid Guid { get; private set; }
        /// <summary>
        /// name
        /// </summary>
        [Required]
        public string Name { get; private set; }
        /// <summary>
        /// whether enabled
        /// </summary>
        [Required]
        [DefaultValue(true)]
        public bool Enable { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string TenantKey { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
        /// updatedata
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
        /// clear the cache
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

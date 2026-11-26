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

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

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
    /// Represents a tenant registration that is excluded from normal tenant filtering.
    /// </summary>
    [Table("TenantInfos")]
    public class TenantInfo : EntityBase<int>, IIgnoreMulitTenant //Legacy comment translated to English.
    {
        /// <summary>
        /// Gets the stable tenant GUID.
        /// </summary>
        [Required]
        public Guid Guid { get; private set; }
        /// <summary>
        /// Gets the display name of the tenant.
        /// </summary>
        [Required]
        public string Name { get; private set; }
        /// <summary>
        /// Gets whether the tenant is enabled for processing.
        /// </summary>
        [Required]
        [DefaultValue(true)]
        public bool Enable { get; private set; }
        /// <summary>
        /// Gets the normalized lookup key used to resolve this tenant.
        /// </summary>
        [Required]
        public string TenantKey { get; private set; }

        /// <summary>
        /// Gets the legacy string tenant identifier exposed through the multi-tenant contract.
        /// The persisted identity is <see cref="Guid"/> and the lookup identity is <see cref="TenantKey"/>.
        /// </summary>
        [NotMapped]
        new public string TenantId { get; private set; }

        /// <summary>
        /// Initializes a tenant registration.
        /// </summary>
        /// <param name="name">The tenant display name.</param>
        /// <param name="enable">Whether the tenant is enabled.</param>
        /// <param name="tenantKey">The tenant lookup key.</param>
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
        /// Updates editable tenant settings and administrator remarks.
        /// </summary>
        /// <param name="dto">The DTO containing replacement tenant settings.</param>
        public void Update(CreateOrUpdate_TenantInfoDto dto)
        {
            Name = dto.Name;
            Enable = dto.Enable;
            TenantKey = dto.TenantKey;
            AdminRemark = dto.AdminRemark;
        }

        /// <summary>
        /// Clears the cached enabled-tenant collection after a tenant change.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve the tenant cache.</param>
        /// <returns>A task that completes after cache removal.</returns>
        public async Task ClearCache(IServiceProvider serviceProvider)
        {
            var fullTenantInfoCache = serviceProvider.GetRequiredService<FullTenantInfoCache>();
            await fullTenantInfoCache.RemoveCacheAsync();
        }
    }
}

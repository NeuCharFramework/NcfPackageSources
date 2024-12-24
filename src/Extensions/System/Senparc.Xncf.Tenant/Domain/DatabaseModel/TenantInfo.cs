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
    /// 租户信息
    /// </summary>
    [Table("TenantInfos")]
    public class TenantInfo : EntityBase<int>, IIgnoreMulitTenant //TODO:可以直接继承不使用多租户的基类
    {
        /// <summary>
        /// 全局唯一编号（自动分配）
        /// </summary>
        [Required]
        public Guid Guid { get; private set; }
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        public string Name { get; private set; }
        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        [DefaultValue(true)]
        public bool Enable { get; private set; }
        /// <summary>
        /// 匹配域名、URL、Head的参数
        /// </summary>
        [Required]
        public string TenantKey { get; private set; }

        /// <summary>
        /// 此属性已经取消和数据库的映射
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
        /// 更新数据
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
        /// 清除缓存
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

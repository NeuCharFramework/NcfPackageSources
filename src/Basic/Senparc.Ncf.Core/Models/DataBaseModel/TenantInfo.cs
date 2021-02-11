using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// 租户信息
    /// </summary>
    public class TenantInfo : EntityBase<int>
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
        public string TenantKey { get; private set; }

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
    }
}

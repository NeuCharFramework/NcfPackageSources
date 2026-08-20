/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IMultiTenancy.cs
    文件功能描述：IMultiTenancy 相关实现
    
    
    创建标识：Senparc - 20201223
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 多租户接口
    /// </summary>
    public interface IMultiTenancy
    {
        /// <summary>
        /// 租户Id
        /// </summary>
        public int TenantId { get; set; }
    }
}

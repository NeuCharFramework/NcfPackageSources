/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CustomerResourceAttribute.cs
    文件功能描述：CustomerResourceAttribute 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{
    public class CustomerResourceAttribute : Attribute
    {
        public string[] ResourceCodes { get; set; }

        public CustomerResourceAttribute(params string[] resuouceCodes)
        {
            ResourceCodes = resuouceCodes;
        }
    }

    /// <summary>
    /// 不进行权限校验
    /// </summary>
    public class IgnoreAuthAttribute: Attribute, IFilterMetadata
    {

    }
}

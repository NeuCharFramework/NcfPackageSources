/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BaseAdminWeixinManagerModel.cs
    文件功能描述：BaseAdminWeixinManagerModel 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager
{
    public class BaseAdminWeixinManagerModel : Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public BaseAdminWeixinManagerModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }
    }
}

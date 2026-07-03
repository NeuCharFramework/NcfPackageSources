/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Service;
using System;

namespace Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}

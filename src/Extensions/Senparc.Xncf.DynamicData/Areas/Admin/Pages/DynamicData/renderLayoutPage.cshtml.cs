/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：renderLayoutPage.cshtml.cs
    文件功能描述：renderLayoutPage.cshtml 相关实现
    
    
    创建标识：Senparc - 20240924
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.DynamicData.Areas.Admin.Pages.DynamicData
{
    public class renderLayoutPageModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public renderLayoutPageModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}

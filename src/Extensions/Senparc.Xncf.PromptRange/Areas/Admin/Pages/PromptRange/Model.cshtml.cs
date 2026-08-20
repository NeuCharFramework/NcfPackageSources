/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Model.cshtml.cs
    文件功能描述：Model.cshtml 相关实现
    
    
    创建标识：Senparc - 20231021
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.PromptRange.Areas.Admin.Pages.PromptRange
{
    public class ModelModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public ModelModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }

        public void OnGet()
        {
        }
    }
}

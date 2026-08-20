/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RecallTest.cshtml.cs
    文件功能描述：RecallTest.cshtml 相关实现
    
    
    创建标识：Senparc - 20260215
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBase
{
    public class RecallTestModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public RecallTestModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            CurrentMenu = "RecallTest";
        }

        public Task OnGetAsync()
        {
            return Task.CompletedTask;
        }
    }
}

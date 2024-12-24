using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.PromptRange.Areas.Admin.Pages.PromptRange
{
    public class PromptModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public PromptModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }

        public void OnGet()
        {
        }
    }
}

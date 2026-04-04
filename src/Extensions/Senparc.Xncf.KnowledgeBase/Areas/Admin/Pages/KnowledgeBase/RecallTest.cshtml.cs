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

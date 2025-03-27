using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.KnowledgeBase.Areas.KnowledgeBase.Pages
{
    public class KnowledgeBase : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public KnowledgeBase(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {

        }
    }
}

using Senparc.Ncf.Service;
using System;

namespace Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>
        /// Handles the template module administration page GET request.
        /// </summary>
        public void OnGet()
        {
        }
    }
}

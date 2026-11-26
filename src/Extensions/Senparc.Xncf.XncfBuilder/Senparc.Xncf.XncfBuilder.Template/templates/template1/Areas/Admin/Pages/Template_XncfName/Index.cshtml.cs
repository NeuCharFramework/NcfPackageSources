using Senparc.Ncf.Service;
using System;

namespace Template_OrgName.Xncf.Template_XncfName.Areas.Template_XncfName.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        /// <summary>Initializes the template module index page.</summary>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        /// <summary>Initializes the template module index request.</summary>
        public void OnGet()
        {
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.Mvc.UI;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;

namespace Senparc.Ncf.AreaBase.Admin//  Senparc.Areas.Admin
{

    public interface IAdminPageModelBase : IPageModelBase
    {
        AdminWorkContext AdminWorkContext { get; set; }
        string Uid { get; set; }
        List<IXncfRegister> XncfRegisterList { get; }

        FullSystemConfig FullSystemConfig { get; set; }

        IActionResult RenderError(string message);
    }

    //Temporarily cancel permission verification
    //[ServiceFilter(typeof(AuthenticationAsyncPageFilterAttribute))]
    [AdminAuthorize("AdminOnly")]
    public class AdminPageModelBase : PageModelBase, IAdminPageModelBase
    {
        /// <summary>
        ///Storage related user information
        /// </summary>
        public virtual AdminWorkContext AdminWorkContext { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Uid { get; set; }

        /// <summary>
        /// List of all XncfRegisters (including those not yet registered)
        /// </summary>
        public virtual List<IXncfRegister> XncfRegisterList => Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList;


        public virtual IActionResult RenderError(string message)
        {
            //Keep original controller and action information
            //ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            //ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return Page();//TODO: Set a specific error page

            //return View("Error", new Error_ExceptionVD
            //{
            //    //HandleErrorInfo = new HandleErrorInfo(new Exception(message), Url.RequestContext.RouteData.GetRequiredString("controller"), Url.RequestContext.RouteData.GetRequiredString("action"))
            //});
        }
    }
}

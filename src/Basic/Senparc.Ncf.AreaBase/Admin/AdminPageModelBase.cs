/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminPageModelBase.cs
    File description: AdminPageModelBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.22.0-preview3 Unify AdminOnly authorization policy naming and provide host registration extension

    Modified by: Senparc - 20261119
    Change description: v0.23.12 Improved administration page and authorization support

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Authorization;
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

    //Temporarily revoke permission validation
    //[ServiceFilter(typeof(AuthenticationAsyncPageFilterAttribute))]
    [AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class AdminPageModelBase : PageModelBase, IAdminPageModelBase
    {
        /// <summary>
        /// Store related user information
        /// </summary>
        public virtual AdminWorkContext AdminWorkContext { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Uid { get; set; }

        /// <summary>
        /// All XncfRegister list tables (including those not yet registered)
        /// </summary>
        public virtual List<IXncfRegister> XncfRegisterList => Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList;


        public virtual IActionResult RenderError(string message)
        {
            //preserve the original controller and action information
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

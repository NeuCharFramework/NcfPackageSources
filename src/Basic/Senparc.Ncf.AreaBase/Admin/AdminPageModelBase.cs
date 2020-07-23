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
        List<IXscfRegister> XscfRegisterList { get; }

        FullSystemConfig FullSystemConfig { get; set; }

        IActionResult RenderError(string message);
    }

    //暂时取消权限验证
    //[ServiceFilter(typeof(AuthenticationAsyncPageFilterAttribute))]
    [AdminAuthorize("AdminOnly")]
    public class AdminPageModelBase : PageModelBase, IAdminPageModelBase
    {
        /// <summary>
        /// 存储相关用户信息
        /// </summary>
        public virtual AdminWorkContext AdminWorkContext { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Uid { get; set; }

        /// <summary>
        /// 所有 XscfRegister 列表（包括还未注册的）
        /// </summary>
        public virtual List<IXscfRegister> XscfRegisterList => Senparc.Ncf.XncfBase.Register.RegisterList;


        public virtual IActionResult RenderError(string message)
        {
            //保留原有的controller和action信息
            //ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            //ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return Page();//TODO：设定一个特定的错误页面

            //return View("Error", new Error_ExceptionVD
            //{
            //    //HandleErrorInfo = new HandleErrorInfo(new Exception(message), Url.RequestContext.RouteData.GetRequiredString("controller"), Url.RequestContext.RouteData.GetRequiredString("action"))
            //});
        }
    }
}

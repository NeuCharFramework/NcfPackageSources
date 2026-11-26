/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: LoginController.cs
    File description: LoginController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Senparc.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using Models.VD;
    using Senparc.Ncf.Core.Models;
    using Microsoft.AspNetCore.Mvc;
    using Senparc.Ncf.Core.Extensions;
    using Senparc.CO2NET.Extensions;
    using Senparc.Ncf.Service;

    [AllowAnonymous]
    public class LoginController : BaseAdminController
    {
        private readonly AdminUserInfoService _userInfoService;
        /// <summary>
        /// Initializes the anonymous legacy administrator login controller.
        /// </summary>
        /// <param name="userInfoService">The administrator authentication service.</param>
        public LoginController(AdminUserInfoService userInfoService)
        {
            this._userInfoService = userInfoService;
        }

        /// <summary>
        /// Shows the login page or redirects an already authenticated administrator
        /// to the requested local destination.
        /// </summary>
        /// <param name="returnUrl">Optional URL to return to after authentication.</param>
        /// <returns>The login view or a redirect result.</returns>
        public async Task<IActionResult> Index(string returnUrl)
        {
            //Legacy comment translated to English.
            var authenticate = await HttpContext.AuthenticateAsync(AdminAuthorizeAttribute.AuthenticationScheme);

            if (authenticate.Succeeded)
            {
                if (returnUrl.IsNullOrEmpty())
                {
                    return RedirectToAction("Index", "Home");
                }
                return Redirect(returnUrl.UrlDecode());
            }
            Login_IndexVD vd = new Login_IndexVD
            {
                ReturnUrl = returnUrl,
                MessagerList = new List<Messager>(),
                IsLogined = this.HttpContext.User.Identity.IsAuthenticated,
            };
          
            return View(vd);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        /// <summary>
        /// Validates administrator credentials and redirects to the requested destination
        /// when authentication succeeds.
        /// </summary>
        /// <param name="vdForm">The posted login form.</param>
        /// <returns>The login view on failure or a redirect on success.</returns>
        public ActionResult Index(Login_IndexVD vdForm)
        {
            if (!ModelState.IsValid)
            {
                return View(vdForm);
            }
            string errorMsg = null;

            var userInfo = _userInfoService.GetUserInfo(vdForm.Name);
            if (userInfo == null)
            {
                errorMsg = "账号或密码错误！错误代码：101。";
            }
            else if (_userInfoService.TryLogin(vdForm.Name, vdForm.Password, true) == null)
            {
                errorMsg = "账号或密码错误！错误代码：102。";
            }

            if (!errorMsg.IsNullOrEmpty() || !ModelState.IsValid)
            {
                vdForm.MessagerList = new List<Messager>
                {
                    new Messager(Senparc.Ncf.Core.Enums.MessageType.danger, errorMsg)
                };
                return View(vdForm);
            }

            if (vdForm.ReturnUrl.IsNullOrEmpty())
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(vdForm.ReturnUrl.UrlDecode());
        }

        /// <summary>Logs out the current administrator and returns to the login action.</summary>
        /// <returns>A redirect to the login page.</returns>
        public ActionResult Logout()
        {
            _userInfoService.Logout();
            return RedirectToAction("Index");
        }
    }
}

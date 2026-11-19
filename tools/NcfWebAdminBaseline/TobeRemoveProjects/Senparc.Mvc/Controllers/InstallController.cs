/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: InstallController.cs
    File description: InstallController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Senparc.Service;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Service;
using Senparc.Core.Models.VD;

namespace Senparc.Mvc.Controllers
{
    public class InstallController : Controller
    {
        private readonly AdminUserInfoService _accountInfoService;
        private readonly SystemConfigService _systemConfigService;
        public InstallController(AdminUserInfoService accountService, SystemConfigService systemConfigService)
        {
            _accountInfoService = accountService;
            _systemConfigService = systemConfigService;
        }
        public IActionResult Index()
        {
            var adminUserInfo = _accountInfoService.Init(out string userName, out string password);

            _systemConfigService.Init();

            if (adminUserInfo == null)
            {
                return new StatusCodeResult(404);
            }

            var vd = new Install_IndexVD()
            {
                AdminUserName = userName,
                AdminPassword = password//Legacy comment translated to English.
            };
            return View(vd);
        }
    }
}

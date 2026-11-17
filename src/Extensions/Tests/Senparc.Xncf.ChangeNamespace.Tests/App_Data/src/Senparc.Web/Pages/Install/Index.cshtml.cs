using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models.VD;
using Senparc.Service;

namespace Senparc.Web.Pages.Install
{
    public class IndexModel : PageModel //Legacy comment translated to English.
    {

        private readonly AdminUserInfoService _accountInfoService;
        private readonly SystemConfigService _systemConfigService;
        private readonly SysMenuService _sysMenuService;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AdminUserName { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AdminPassword { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Namespace { get; set; }

        public int Step { get; set; }


        public IndexModel(AdminUserInfoService accountService, SystemConfigService systemConfigService, SysMenuService sysMenuService)
        {
            _accountInfoService = accountService;
            _sysMenuService = sysMenuService;
            _systemConfigService = systemConfigService;
        }

        public IActionResult OnGet()
        {
            var adminUserInfo = _accountInfoService.GetObject(z => true);//Legacy comment translated to English.
            if (adminUserInfo != null)
            {
                return new StatusCodeResult(404);
                //base.Response.StatusCode = 404;
                //return; 
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            var adminUserInfo = _accountInfoService.Init(out string userName, out string password);//Legacy comment translated to English.

            if (adminUserInfo == null)
            {
                return new StatusCodeResult(404);
                //base.Response.StatusCode = 404;
                //return; 
            }
            else
            {
                Step = 1;
                _systemConfigService.Init();//Legacy comment translated to English.
                _sysMenuService.Init();

                AdminUserName = userName;
                AdminPassword = password;//Legacy comment translated to English.

                return Page();
            }
        }
    }
}
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.Ncf.AreaBase.Admin.Filters;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAuth]
    [AllowAnonymous]
    public class SenparcTrace_IndexModel(IServiceProvider serviceProvider) : BaseAdminPageModel(serviceProvider)
    {

        //public List<string> DateList { get; private set; }

        public IActionResult OnGet()
        {
            if (!IsAdminSession())
            {
                return Challenge();
            }
            Response.Headers.CacheControl = "no-store";
            //DateList = SenparcTraceHelper.GetLogDate();
            return Page();
        }

        public IActionResult OnGetList(int pageIndex = 1, int pageSize = 10)
        {
            if (!IsAdminSession())
            {
                return Unauthorized();
            }
            Response.Headers.CacheControl = "no-store";
            return Ok(SenparcTraceHelper.GetLogFiles(pageIndex, pageSize));
        }

        private bool IsAdminSession()
        {
            return User?.Identity?.IsAuthenticated == true;
        }
    }
}

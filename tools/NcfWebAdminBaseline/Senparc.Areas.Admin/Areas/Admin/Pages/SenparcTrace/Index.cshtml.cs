using System;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.SenparcTraceManager;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class SenparcTrace_IndexModel(IServiceProvider serviceProvider) : BaseAdminPageModel(serviceProvider)
    {

        //public List<string> DateList { get; private set; }

        public void OnGet()
        {
            //DateList = SenparcTraceHelper.GetLogDate();
        }

        public IActionResult OnGetList(int pageIndex = 1, int pageSize = 10)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(SenparcTraceHelper.GetLogFiles(pageIndex, pageSize));
        }
    }
}

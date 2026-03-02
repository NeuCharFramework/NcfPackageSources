using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.Ncf.AreaBase.Admin.Filters;
using System.Linq;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAuth]
    public class SenparcTrace_IndexModel(IServiceProvider serviceProvider) : BaseAdminPageModel(serviceProvider)
    {

        //public List<string> DateList { get; private set; }

        public void OnGet()
        {
            //DateList = SenparcTraceHelper.GetLogDate();
        }

        public IActionResult OnGetList(string keyword, int pageIndex = 1, int pageSize = 10)
        {
            var dateList = SenparcTraceHelper.GetLogDate() ?? new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(keyword))
            {
                dateList = dateList
                    .Where(z => z != null && z.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            var count = dateList.Count;
            var list = dateList
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new { count, pageIndex, list });
        }
    }
}

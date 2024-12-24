using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class AdminUserInfo_IndexModel : BaseAdminPageModel
    {
        private readonly AdminUserInfoService _adminUserInfoService;
        public PagedList<AdminUserInfo> AdminUserInfoList { get; set; }

        /// <summary>
        /// 属性绑定，支持GET
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 排序方法
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string OrderField { get; set; } = "AddTime Desc,Id";

        public AdminUserInfo_IndexModel(AdminUserInfoService adminUserInfoService)
        {
            _adminUserInfoService = adminUserInfoService;
        }

        //[Filters.CustomerResourceFilter("Add")]
        public async Task<IActionResult> OnGetAsync()
        {
            var seh = new SenparcExpressionHelper<AdminUserInfo>();
            var where = seh.BuildWhereExpression();
            var admins = await _adminUserInfoService.GetObjectListAsync(PageIndex, 20, where, OrderField);
            AdminUserInfoList = admins;
            return null;
        }

        public IActionResult OnPostDemo()
        {
            return Ok(null);
        }

        public IActionResult OnPost(int[] ids)
        {
            foreach (var id in ids)
            {
                _adminUserInfoService.DeleteObject(id);
            }

            return RedirectToPage("./Index");
        }
    }
}
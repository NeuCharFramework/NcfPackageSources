/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AutoValidateAntiforgeryToken]
    public class RoleIndexModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;

        public RoleIndexModel(IServiceProvider serviceProvider, SysRoleService sysRoleService)
            : base(serviceProvider)
        {
            CurrentMenu = "Role";
            this._sysRoleService = sysRoleService;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 
        /// </summary>
        public PagedList<SysRole> SysRoles { get; set; }

        public void OnGet()
        {
            //SysRoles = new ;// await _sysRoleService.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
        }

        /// <summary>
        /// Handler=List
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="orderField">XXX DESC,YYY ASC</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-search")]
        public async Task<IActionResult> OnGetListAsync(string roleName, string orderField, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<SysRole>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(roleName), _ => _.RoleName.Contains(roleName));
            var where = seh.BuildWhereExpression();
            var admins = await _sysRoleService.GetObjectListAsync(pageIndex, pageSize, where, orderField);

            //AdminUserInfoList = admins;
            return Ok(new
            {
                admins.TotalCount,
                admins.PageIndex,
                List =
                admins.Select(_ => new { _.Id, _.LastUpdateTime, _.Remark, _.RoleName, _.RoleCode, _.AddTime, _.AdminRemark, _.Enabled })
            });
        }

        //public IActionResult OnPostDelete(string[] ids)
        //{
        //    foreach (var id in ids)
        //    {
        //        _sysRoleService.DeleteObject(_ => _.Id == id);
        //    }

        //    return RedirectToPage("./Index");
        //}

        /// <summary>
        /// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-delete")]
        public IActionResult OnPostDelete([FromBody] string[] ids)
        {
            foreach (var id in ids)
            {
                _sysRoleService.DeleteObject(_ => _.Id == id);
            }
            return Ok(ids.Length);
        }
    }
}

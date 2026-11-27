/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
        /// Gets or sets the paged role records displayed by the view.
        /// </summary>
        public PagedList<SysRole> SysRoles { get; set; }

        /// <summary>
        /// Handles the initial role-list page request.
        /// The list data is loaded by the client-side list handler.
        /// </summary>
        public void OnGet()
        {
            //SysRoles = new ;// await _sysRoleService.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
        }

        /// <summary>
        /// Queries roles by optional name, applies the requested sort expression,
        /// and returns a projected page suitable for the administration list.
        /// </summary>
        /// <param name="roleName">Optional text matched against the role name.</param>
        /// <param name="orderField">A service-supported sort expression such as <c>RoleName DESC</c>.</param>
        /// <param name="pageIndex">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of roles to include.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing paging metadata and role fields.</returns>
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
        /// Deletes the roles identified by the request body.
        /// Authorization is enforced by the role-delete resource filter.
        /// </summary>
        /// <param name="ids">The role identifiers to delete.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the number of supplied identifiers.</returns>
        /// <exception cref="NullReferenceException">Thrown when no identifier array is bound to the request.</exception>
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

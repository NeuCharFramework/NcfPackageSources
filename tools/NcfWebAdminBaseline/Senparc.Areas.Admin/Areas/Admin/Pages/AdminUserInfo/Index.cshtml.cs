/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20241028

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
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AutoValidateAntiforgeryToken]
    public class AdminUserInfo_IndexModel(IServiceProvider serviceProvider, AdminUserInfoService adminUserInfoService) 
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
        public PagedList<AdminUserInfo> AdminUserInfoList { get; set; }

        /// <summary>
        /// Gets or sets the one-based page number used by the administrator list query.
        /// The value is bound from the query string and defaults to the first page.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets or sets the sort expression passed to the administrator list query.
        /// The default orders by creation time in descending order and then by identifier.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string OrderField { get; set; } = "AddTime Desc,Id";

        /// <summary>
        /// Handles the initial administrator-list page request.
        /// The list is loaded by the client-side list handler, so this method completes
        /// without producing an additional server-side model payload.
        /// </summary>
        /// <returns>The Razor Page result for the administrator-list view.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// Queries administrators whose user name contains the supplied search text and
        /// returns the requested page together with its total-count metadata.
        /// </summary>
        /// <param name="adminUserInfoName">Optional search text applied to the user name.</param>
        /// <param name="pageIndex">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of records to include in the page.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing page metadata and administrator records.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Propagated when the underlying service rejects an invalid page index or page size.
        /// </exception>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-search")]
        public async Task<IActionResult> OnGetListAsync(string adminUserInfoName, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<AdminUserInfo>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(adminUserInfoName), _ => _.UserName.Contains(adminUserInfoName));
            var where = seh.BuildWhereExpression();
            var admins = await _adminUserInfoService.GetObjectListAsync(pageIndex, pageSize, where, OrderField);
            return Ok(new { admins.TotalCount, admins.PageIndex, List = admins.AsEnumerable() });
        }

        /// <summary>
        /// Deletes the administrators identified by the request body and returns the number
        /// of identifiers processed. Authorization is enforced by the page resource filter.
        /// </summary>
        /// <param name="ids">The administrator identifiers to delete.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the number of supplied identifiers.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown if the request body is null because the handler requires an identifier array.
        /// </exception>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-delete")]
        public IActionResult OnPostDelete([FromBody]int[] ids)
        {
            foreach (var id in ids)
            {
                _adminUserInfoService.DeleteObject(id);
            }
            return Ok(ids.Length);
            //return RedirectToPage("./Index");
        }
    }
}

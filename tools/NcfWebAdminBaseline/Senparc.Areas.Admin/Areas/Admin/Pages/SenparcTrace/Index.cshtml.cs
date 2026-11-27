/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Provides i ndex.cshtml functionality.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

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

        /// <summary>
        /// Renders the trace-log index after requiring an authenticated administrator
        /// session and disables browser caching for log metadata.
        /// </summary>
        /// <returns>The trace index page or an authentication challenge.</returns>
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

        /// <summary>
        /// Returns a paged list of available trace-log files for an authenticated
        /// administrator without allowing browser caching.
        /// </summary>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <param name="pageSize">The requested page size.</param>
        /// <returns>The log-file page or HTTP 401 when no administrator session exists.</returns>
        public IActionResult OnGetList(int pageIndex = 1, int pageSize = 10)
        {
            if (!IsAdminSession())
            {
                return Unauthorized();
            }
            Response.Headers.CacheControl = "no-store";
            return Ok(SenparcTraceHelper.GetLogFiles(pageIndex, pageSize));
        }

        /// <summary>
        /// Checks whether the current request has an authenticated principal.
        /// </summary>
        /// <returns><see langword="true"/> when the current identity is authenticated.</returns>
        private bool IsAdminSession()
        {
            return User?.Identity?.IsAuthenticated == true;
        }
    }
}

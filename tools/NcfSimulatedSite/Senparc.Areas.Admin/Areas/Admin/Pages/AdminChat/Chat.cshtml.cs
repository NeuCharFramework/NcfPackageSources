/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Chat.cshtml.cs
    File description: Chat.cshtml.cs implementation and project behavior.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages.AdminChat
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    [Ncf.AreaBase.Admin.Filters.IgnoreAuth]
    public class ChatModel : BaseAdminPageModel
    {
        private readonly AdminChatSessionService _sessionService;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider;

        public ChatModel(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            IAdminWorkContextProvider adminWorkContextProvider)
            : base(serviceProvider)
        {
            _sessionService = sessionService;
            _adminWorkContextProvider = adminWorkContextProvider;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int SessionId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string InitialMessage { get; set; }

        /// <summary>
        /// currentuserID
        /// </summary>
        public int CurrentUserId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ModuleUids { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var adminWorkContext = _adminWorkContextProvider.GetAdminWorkContext();
            CurrentUserId = adminWorkContext.AdminUserId;
            IsSuperAdmin = (adminWorkContext.RoleCodes ?? Enumerable.Empty<string>())
                .Any(role => string.Equals(role?.Trim(), Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, System.StringComparison.OrdinalIgnoreCase));

            if (SessionId > 0)
            {
                var session = await _sessionService.GetSessionByIdAsync(SessionId, CurrentUserId);
                if (session == null)
                {
                    return RedirectToPage("../Index");
                }
            }

            return Page();
        }
    }
}

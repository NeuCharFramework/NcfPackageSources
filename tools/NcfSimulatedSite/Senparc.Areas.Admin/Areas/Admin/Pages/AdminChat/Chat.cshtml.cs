using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages.AdminChat
{
    /// <summary>
    /// Consistent with pages such as SenparcTrace/Index: <see cref="Ncf.AreaBase.Admin.Filters.IgnoreAuth"/> Skip menu URL verification;
    /// Login and AdminOnly are handled uniformly by <see cref="BaseAdminPageModel"/> and Cookie middleware, and no Login jump is written on this page.
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
        ///Session ID (URL parameter)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int SessionId { get; set; }

        /// <summary>
        /// Initial message (URL parameters, optional)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string InitialMessage { get; set; }

        /// <summary>
        ///Current user ID
        /// </summary>
        public int CurrentUserId { get; set; }

        /// <summary>
        /// List of module UIDs (comma separated string)
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ModuleUids { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;

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

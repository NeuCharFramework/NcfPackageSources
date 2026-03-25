using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages.AdminChat
{
    [Ncf.AreaBase.Admin.Filters.AdminAuthorize]
    public class ChatModel : BaseAdminPageModel
    {
        private readonly AdminChatSessionService _sessionService;

        public ChatModel(IServiceProvider serviceProvider, AdminChatSessionService sessionService) 
            : base(serviceProvider)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// 会话ID（URL参数）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int SessionId { get; set; }

        /// <summary>
        /// 初始消息（URL参数，可选）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string InitialMessage { get; set; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        public int CurrentUserId { get; set; }

        /// <summary>
        /// 模块 UID 列表（逗号分隔的字符串）
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ModuleUids { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 通过 AdminWorkContext 获取当前用户ID
            CurrentUserId = AdminWorkContext?.AdminUserId ?? 0;

            if (CurrentUserId <= 0)
            {
                return RedirectToPage("/Admin/Login");
            }

            if (SessionId > 0)
            {
                var session = await _sessionService.GetSessionByIdAsync(SessionId, CurrentUserId);
                if (session == null)
                {
                    return RedirectToPage("/Admin/Index");
                }
            }

            return Page();
        }
    }
}

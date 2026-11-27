/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Chat.cshtml.cs
    File description: Chat.cshtml.cs implementation and project behavior.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261125
    Change description: v0.12.29 Improved AdminChat conversation loading and command parameter feedback.

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.ChatTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages.AdminChat
{
    /// <summary>
    /// Provides the Razor Page model for the administrator chat workspace.
    /// It prepares the current administrator context and the registered chat-tool metadata
    /// needed by the client before a conversation is opened.
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
        /// Gets or sets the optional chat session identifier supplied in the query string.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int SessionId { get; set; }

        /// <summary>
        /// Gets or sets an optional message that the client may use to initialize the chat input.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string InitialMessage { get; set; }

        /// <summary>
        /// currentuserID
        /// </summary>
        public int CurrentUserId { get; set; }

        /// <summary>
        /// Gets or sets whether the current administrator has the super-administrator role.
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// Gets or sets the optional comma-separated module identifiers used to scope chat tools.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ModuleUids { get; set; }

        /// <summary>
        /// Gets the chat-tool descriptions exposed to the client-side command picker.
        /// </summary>
        public IReadOnlyList<ChatToolCommandViewModel> ChatTools { get; private set; }
            = Array.Empty<ChatToolCommandViewModel>();

        /// <summary>
        /// Builds the client-facing chat-tool metadata, initializes the current administrator
        /// context, and verifies that a requested session belongs to that administrator.
        /// An invalid session redirects to the administration dashboard.
        /// </summary>
        /// <returns>The chat page result, or a redirect when the requested session is unavailable.</returns>
        /// <exception cref="InvalidOperationException">
        /// Propagated if the work-context provider cannot establish an administrator context.
        /// </exception>
        public async Task<IActionResult> OnGetAsync()
        {
            ChatTools = ChatToolRegistry.GetChatTools()
                .Select(tool => new ChatToolCommandViewModel(
                    tool.Command,
                    tool.Label,
                    tool.Hint,
                    tool.ModuleName,
                    tool.ModuleUid,
                    tool.Parameters
                        .OrderBy(parameter => parameter.Order)
                        .Select(parameter => new ChatToolParameterViewModel(
                            parameter.Name,
                            parameter.Label,
                            parameter.Hint,
                            parameter.Placeholder,
                            parameter.Prefix,
                            parameter.Required,
                            parameter.IsPositional,
                            parameter.AllowFreeform,
                            parameter.OptionsProviderType != null,
                            parameter.Options.Select(option => new ChatToolParameterOptionViewModel(
                                option.Value,
                                option.Label,
                                option.Description))
                            .ToList()))
                        .ToList()))
                .ToList();

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

    /// <summary>
    /// Describes one registered chat command and its ordered parameters.
    /// </summary>
    public sealed record ChatToolCommandViewModel(
        string Command,
        string Label,
        string Hint,
        string ModuleName,
        string ModuleUid,
        IReadOnlyList<ChatToolParameterViewModel> Parameters);

    /// <summary>
    /// Describes one chat-command parameter, including validation and option-provider metadata.
    /// </summary>
    public sealed record ChatToolParameterViewModel(
        string Name,
        string Label,
        string Hint,
        string Placeholder,
        string Prefix,
        bool Required,
        bool IsPositional,
        bool AllowFreeform,
        bool HasOptionsProvider,
        IReadOnlyList<ChatToolParameterOptionViewModel> Options);

    /// <summary>
    /// Represents one selectable value exposed for a chat-command parameter.
    /// </summary>
    public sealed record ChatToolParameterOptionViewModel(
        string Value,
        string Label,
        string Description);
}

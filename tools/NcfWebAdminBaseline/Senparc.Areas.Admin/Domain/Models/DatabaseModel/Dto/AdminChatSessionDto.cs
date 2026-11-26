/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSessionDto.cs
    File description: AdminChatSessionDto.cs related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// Represents an administrator chat session and its client-visible message,
    /// module, and workflow collections.
    /// </summary>
    public class AdminChatSessionDto : DtoBase<int>
    {
        /// <summary>
        /// Gets or sets the user-visible title of the session.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// userID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the current lifecycle status of the session.
        /// </summary>
        public ChatSessionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the most recent message.
        /// </summary>
        public DateTime LastMessageTime { get; set; }

        public string WorkspaceId { get; set; }

        public string WorkspaceName { get; set; }

        public bool WorkspaceWriteEnabled { get; set; }

        public bool WorkspaceCliEnabled { get; set; }

        public int? SortOrder { get; set; }

        public bool HasUnread { get; set; }

        /// <summary>
        /// Gets or sets the messages belonging to the session.
        /// </summary>
        public List<AdminChatMessageDto> Messages { get; set; }

        /// <summary>
        /// Gets or sets the modules attached to the session.
        /// </summary>
        public List<AdminChatSessionModuleDto> Modules { get; set; }

        /// <summary>
        /// Gets or sets the workflows attached to the session.
        /// </summary>
        public List<AdminChatSessionWorkflowDto> Workflows { get; set; }

        /// <summary>
        /// Initializes a session DTO with non-null message, module, and workflow collections.
        /// </summary>
        public AdminChatSessionDto() 
        {
            Messages = new List<AdminChatMessageDto>();
            Modules = new List<AdminChatSessionModuleDto>();
            Workflows = new List<AdminChatSessionWorkflowDto>();
        }

        /// <summary>
        /// Creates a session DTO from a persisted <see cref="AdminChatSession"/> entity.
        /// </summary>
        /// <param name="entity">The persisted session; <see langword="null"/> returns <see langword="null"/>.</param>
        /// <returns>A populated session DTO, or <see langword="null"/> when <paramref name="entity"/> is null.</returns>
        public static AdminChatSessionDto CreateFromEntity(AdminChatSession entity)
        {
            if (entity == null) return null;

            return new AdminChatSessionDto
            {
                //Legacy comment translated to English.
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                //Legacy comment translated to English.
                Title = entity.Title,
                UserId = entity.UserId,
                Status = entity.Status,
                LastMessageTime = entity.LastMessageTime,
                WorkspaceId = entity.WorkspaceId,
                WorkspaceWriteEnabled = entity.WorkspaceWriteEnabled,
                WorkspaceCliEnabled = entity.WorkspaceCliEnabled,
                SortOrder = entity.SortOrder,
                HasUnread = entity.HasUnread
            };
        }
    }

    /// <summary>
    /// Carries the values required to create an administrator chat session.
    /// </summary>
    public class CreateChatSessionInputDto
    {
        public bool UseMemory { get; set; }
        public Senparc.Areas.Admin.Domain.Services.AdminChatMemoryMode? MemoryMode { get; set; }
        /// <summary>
        /// Gets or sets the first user message, when the session is created with one.
        /// </summary>
        public string InitialMessage { get; set; }

        /// <summary>
        /// Gets or sets the optional session title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the optional AI model identifier.
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        /// Gets or sets the module UIDs to attach to the new session.
        /// </summary>
        public List<string> ModuleUids { get; set; }

        /// <summary>
        /// Gets or sets the workflow identifiers to attach to the new session.
        /// </summary>
        public List<int> WorkflowIds { get; set; }

        /// <summary>
        /// Gets or sets the execution mode used by the new session.
        /// </summary>
        public AdminChatMode Mode { get; set; } = AdminChatMode.Simple;
        public int MaxOutputTokens { get; set; } = 2000;
        public AdminChatHarnessOptions HarnessOptions { get; set; } = new();

        public string WorkspaceId { get; set; }

        public bool WorkspaceWriteEnabled { get; set; }

        public bool WorkspaceCliEnabled { get; set; }

        /// <summary>
        /// Initializes a session input DTO with empty module and workflow collections.
        /// </summary>
        public CreateChatSessionInputDto()
        {
            ModuleUids = new List<string>();
            WorkflowIds = new List<int>();
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMessageDto.cs
    File description: Related implementation.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261105
    Change description: v0.12.26 Updated module behavior and related support.

----------------------------------------------------------------*/
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Abstractions.References;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminChatMessageDto : DtoBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public ChatMessageRoleType RoleType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Content { get; set; }
        public IReadOnlyList<AdminChatAttachment> Attachments { get; set; } = Array.Empty<AdminChatAttachment>();
        public IReadOnlyList<PromptReference> PromptReferences { get; set; } = Array.Empty<PromptReference>();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public MessageFeedbackType UserFeedback { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ModelIdentifier { get; set; }

        public int? TrajectoryId { get; set; }

        public int? TrajectorySequence { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static AdminChatMessageDto CreateFromEntity(AdminChatMessage entity)
        {
            if (entity == null) return null;

            return new AdminChatMessageDto
            {
                //Legacy comment translated to English.
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                //Legacy comment translated to English.
                SessionId = entity.SessionId,
                RoleType = entity.RoleType,
                Content = entity.Content,
                Attachments = entity.GetAttachments(),
                PromptReferences = entity.GetPromptReferences(),
                Sequence = entity.Sequence,
                UserFeedback = entity.UserFeedback,
                ModelIdentifier = entity.ModelIdentifier,
                TrajectoryId = entity.TrajectoryId,
                TrajectorySequence = entity.TrajectorySequence
            };
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ChatMessageInputDto
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int SessionId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AdminChatMode Mode { get; set; } = AdminChatMode.Simple;
        public int MaxOutputTokens { get; set; }
        public AdminChatHarnessOptions HarnessOptions { get; set; } = new();
        [MaxLength(4)]
        public List<AdminChatAttachmentInput> Attachments { get; set; } = new();
        [MaxLength(32)]
        public List<PromptReference> PromptReferences { get; set; } = new();
    }
}

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

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

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
    /// Represents an administrator chat message projected for transport to the client.
    /// The DTO includes conversation ordering, model provenance, attachments, references,
    /// and optional trajectory metadata.
    /// </summary>
    public class AdminChatMessageDto : DtoBase<int>
    {
        /// <summary>
        /// Gets or sets the identifier of the conversation session containing this message.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Gets or sets the role that produced the message.
        /// </summary>
        public ChatMessageRoleType RoleType { get; set; }

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string Content { get; set; }
        public IReadOnlyList<AdminChatAttachment> Attachments { get; set; } = Array.Empty<AdminChatAttachment>();
        public IReadOnlyList<PromptReference> PromptReferences { get; set; } = Array.Empty<PromptReference>();

        /// <summary>
        /// Gets or sets the sequence number used to order this message within its session.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the feedback state supplied by the administrator.
        /// </summary>
        public MessageFeedbackType UserFeedback { get; set; }

        /// <summary>
        /// Gets or sets the model identifier that generated this message, when available.
        /// </summary>
        public string ModelIdentifier { get; set; }

        public int? TrajectoryId { get; set; }

        public int? TrajectorySequence { get; set; }

        /// <summary>
        /// Creates a transport DTO from a persisted <see cref="AdminChatMessage"/> entity.
        /// Attachment and prompt-reference payloads are expanded through the entity helpers.
        /// </summary>
        /// <param name="entity">The persisted message; <see langword="null"/> returns <see langword="null"/>.</param>
        /// <returns>A populated message DTO, or <see langword="null"/> when <paramref name="entity"/> is null.</returns>
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
    /// Carries the user input required to append a message to an administrator chat session.
    /// </summary>
    public class ChatMessageInputDto
    {
        /// <summary>
        /// Gets or sets the target chat session identifier.
        /// </summary>
        [Required]
        public int SessionId { get; set; }

        /// <summary>
        /// Gets or sets the optional AI model identifier requested for the message.
        /// </summary>
        public int AiModelId { get; set; }

        /// <summary>
        /// Gets or sets the message text submitted by the administrator.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the execution mode used to process the message.
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

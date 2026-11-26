/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMessage.cs
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

    Modified by: Senparc - 20261125
    Change description: v0.12.29 Improved AdminChat conversation loading and command parameter feedback.

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Abstractions.References;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatMessage))]
    [Serializable]
    public class AdminChatMessage : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public ChatMessageRoleType RoleType { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string Content { get; private set; }

        public string AttachmentsJson { get; private set; }
        public string PromptReferencesJson { get; private set; }

        /// <summary>
        /// Deserializes the stored prompt-reference payload and validates that it is well formed.
        /// </summary>
        /// <returns>The stored prompt references, or an empty list when none are stored.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stored JSON does not deserialize to references.</exception>
        public IReadOnlyList<PromptReference> GetPromptReferences() =>
            string.IsNullOrEmpty(PromptReferencesJson) ? Array.Empty<PromptReference>()
                : JsonSerializer.Deserialize<List<PromptReference>>(PromptReferencesJson)
                    ?? throw new InvalidOperationException("Invalid prompt references.");

        /// <summary>
        /// Validates and serializes prompt references against the current message content.
        /// </summary>
        /// <param name="references">The references to store; null or empty input clears the payload.</param>
        /// <exception cref="ArgumentException">Thrown when a reference range is invalid for <see cref="Content"/>.</exception>
        public void SetPromptReferences(IReadOnlyList<PromptReference> references)
        {
            PromptReference.ValidateRanges(Content, references);
            PromptReferencesJson = references?.Count > 0 ? JsonSerializer.Serialize(references) : null;
        }

        /// <summary>
        /// Deserializes the stored attachment payload.
        /// </summary>
        /// <returns>The stored attachments, or an empty list when none are stored.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stored JSON cannot be deserialized.</exception>
        public IReadOnlyList<AdminChatAttachment> GetAttachments() =>
            string.IsNullOrEmpty(AttachmentsJson) ? Array.Empty<AdminChatAttachment>()
                : JsonSerializer.Deserialize<List<AdminChatAttachment>>(AttachmentsJson)
                    ?? throw new InvalidOperationException("消息附件记录无效。");

        /// <summary>
        /// Serializes the message attachments and clears the payload for empty input.
        /// </summary>
        /// <param name="attachments">The attachments or operation results to store.</param>
        /// <exception cref="ArgumentException">Thrown when more than 100 attachments are supplied.</exception>
        public void SetAttachments(IReadOnlyList<AdminChatAttachment> attachments)
        {
            if (attachments?.Count > 100) throw new ArgumentException("每条消息最多关联 100 个附件或操作结果。");
            AttachmentsJson = attachments?.Count > 0 ? JsonSerializer.Serialize(attachments) : null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public MessageFeedbackType UserFeedback { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(100)]
        public string ModelIdentifier { get; private set; }

        /// <summary>
        /// Native Harness Trajectory associated with this assistant message.
        /// </summary>
        public int? TrajectoryId { get; private set; }

        /// <summary>
        /// The last Trajectory event represented by this assistant message.
        /// </summary>
        public int? TrajectorySequence { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual AdminChatSession Session { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private AdminChatMessage() { }

        /// <summary>
        /// Initializes a chat message with session, role, content, sequence, and optional model/trajectory metadata.
        /// </summary>
        /// <param name="sessionId">The owning session identifier.</param>
        /// <param name="roleType">The role that produced the message.</param>
        /// <param name="content">The message content; null becomes an empty string.</param>
        /// <param name="sequence">The message sequence within the session.</param>
        /// <param name="modelIdentifier">The optional model identifier.</param>
        /// <param name="trajectoryId">The optional Harness trajectory identifier.</param>
        /// <param name="trajectorySequence">The optional trajectory-event sequence.</param>
        public AdminChatMessage(
            int sessionId,
            ChatMessageRoleType roleType,
            string content,
            int sequence,
            string modelIdentifier = null,
            int? trajectoryId = null,
            int? trajectorySequence = null)
        {
            SessionId = sessionId;
            RoleType = roleType;
            Content = content ?? string.Empty;
            Sequence = sequence;
            UserFeedback = MessageFeedbackType.None;
            ModelIdentifier = modelIdentifier;
            TrajectoryId = trajectoryId;
            TrajectorySequence = trajectorySequence;
        }

        /// <summary>
        /// Replaces the administrator feedback state and records the entity update time.
        /// </summary>
        /// <param name="feedback">The new feedback value.</param>
        public void SetFeedback(MessageFeedbackType feedback)
        {
            UserFeedback = feedback;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Replaces message content and clears prompt references when the text changes.
        /// </summary>
        /// <param name="newContent">The replacement content; null leaves the current content unchanged.</param>
        public void UpdateContent(string newContent)
        {
            if (newContent != null)
            {
                if (Content != newContent) PromptReferencesJson = null;
                Content = newContent;
                base.SetUpdateTime();
            }
        }

        /// <summary>
        /// Associates the message with a Harness trajectory event and records the update time.
        /// </summary>
        /// <param name="trajectoryId">The optional trajectory identifier.</param>
        /// <param name="trajectorySequence">The optional trajectory-event sequence.</param>
        public void SetTrajectory(int? trajectoryId, int? trajectorySequence)
        {
            TrajectoryId = trajectoryId;
            TrajectorySequence = trajectorySequence;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ChatMessageRoleType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        User = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Assistant = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        System = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum MessageFeedbackType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        None = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Like = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Dislike = 2
    }
}

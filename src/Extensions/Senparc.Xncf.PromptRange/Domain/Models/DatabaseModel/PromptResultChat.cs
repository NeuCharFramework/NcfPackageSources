/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChat.cs
    File description: PromptResultChat related implementation

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    /// Stores one ordered user or assistant message associated with a prompt result.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResultChat))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class PromptResultChat : EntityBase<int>
    {
        /// <summary>Gets the owning prompt-result identifier.</summary>
        [Required]
        public int PromptResultId { get; private set; }

        /// <summary>Gets the author role of the message.</summary>
        [Required]
        public ChatRoleType RoleType { get; private set; }

        /// <summary>Gets the message content.</summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>Gets the sequence within the chat.</summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>Gets or sets the optional user feedback flag.</summary>
        public bool? UserFeedback { get; private set; }

        /// <summary>Gets or sets the optional user score.</summary>
        public decimal? UserScore { get; private set; }

        private PromptResultChat()
        {
        }

        /// <summary>Initializes a chat message for a prompt result.</summary>
        /// <param name="promptResultId">The owning prompt-result identifier.</param>
        /// <param name="roleType">The message author role.</param>
        /// <param name="content">The message content.</param>
        /// <param name="sequence">The sequence within the chat.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
        public PromptResultChat(int promptResultId, ChatRoleType roleType, string content, int sequence)
        {
            PromptResultId = promptResultId;
            RoleType = roleType;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Sequence = sequence;
        }

        /// <summary>Initializes a chat message from its DTO.</summary>
        /// <param name="dto">The DTO values to copy.</param>
        public PromptResultChat(PromptResultChatDto dto)
        {
            Id = dto.Id;
            PromptResultId = dto.PromptResultId;
            RoleType = dto.RoleType;
            Content = dto.Content;
            Sequence = dto.Sequence;
            UserFeedback = dto.UserFeedback;
            UserScore = dto.UserScore;
        }

        /// <summary>Updates the optional user feedback flag.</summary>
        /// <param name="feedback">The feedback value, or <see langword="null"/> to clear it.</param>
        /// <returns>This chat message entity.</returns>
        public PromptResultChat UpdateUserFeedback(bool? feedback)
        {
            UserFeedback = feedback;
            return this;
        }

        /// <summary>Updates the optional user score.</summary>
        /// <param name="score">The score from zero through ten, or <see langword="null"/> to clear it.</param>
        /// <returns>This chat message entity.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="score"/> is outside 0-10.</exception>
        public PromptResultChat UpdateUserScore(decimal? score)
        {
            if (score.HasValue && (score.Value < 0 || score.Value > 10))
            {
                throw new ArgumentOutOfRangeException(nameof(score), "评分必须在 0-10 之间");
            }
            UserScore = score;
            return this;
        }
    }

    /// <summary>Identifies the author role of a prompt-result chat message.</summary>
    public enum ChatRoleType
    {
        /// <summary>The human/user message role.</summary>
        User = 1,

        /// <summary>The assistant/model message role.</summary>
        Assistant = 2
    }
}

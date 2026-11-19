/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChat.cs
    File description: PromptResultChat related implementation

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResultChat))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class PromptResultChat : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int PromptResultId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public ChatRoleType RoleType { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool? UserFeedback { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal? UserScore { get; private set; }

        private PromptResultChat()
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="promptResultId">PromptResult ID</param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public PromptResultChat(int promptResultId, ChatRoleType roleType, string content, int sequence)
        {
            PromptResultId = promptResultId;
            RoleType = roleType;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Sequence = sequence;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="dto"></param>
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

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public PromptResultChat UpdateUserFeedback(bool? feedback)
        {
            UserFeedback = feedback;
            return this;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
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

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ChatRoleType
    {
        /// <summary>
        /// user
        /// </summary>
        User = 1,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Assistant = 2
    }
}

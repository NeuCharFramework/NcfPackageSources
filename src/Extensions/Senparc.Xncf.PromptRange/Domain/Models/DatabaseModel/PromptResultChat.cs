using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    ///PromptResultChat: Conversation history for PromptResult
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResultChat))] //The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class PromptResultChat : EntityBase<int>
    {
        /// <summary>
        ///ID of PromptResult (foreign key)
        /// </summary>
        [Required]
        public int PromptResultId { get; private set; }

        /// <summary>
        /// Dialogue role type: User or Assistant
        /// </summary>
        [Required]
        public ChatRoleType RoleType { get; private set; }

        /// <summary>
        ///Conversation content
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// Conversation order (order within the same PromptResult, starting from 1)
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        /// User feedback: Like (true), Unlike (false), No feedback (null)
        /// </summary>
        public bool? UserFeedback { get; private set; }

        /// <summary>
        /// User rating (0-10 points, optional)
        /// </summary>
        public decimal? UserScore { get; private set; }

        private PromptResultChat()
        {
        }

        /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="promptResultId">ID of PromptResult</param>
        /// <param name="roleType">Dialogue role type</param>
        /// <param name="content">Conversation content</param>
        /// <param name="sequence">Conversation sequence</param>
        public PromptResultChat(int promptResultId, ChatRoleType roleType, string content, int sequence)
        {
            PromptResultId = promptResultId;
            RoleType = roleType;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Sequence = sequence;
        }

        /// <summary>
        ///Create entities from DTO
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
        ///Update user feedback
        /// </summary>
        /// <param name="feedback">Like (true), Unlike (false), Cancel feedback (null)</param>
        /// <returns></returns>
        public PromptResultChat UpdateUserFeedback(bool? feedback)
        {
            UserFeedback = feedback;
            return this;
        }

        /// <summary>
        ///update user ratings
        /// </summary>
        /// <param name="score">Score (0-10 points), null means cancel the score</param>
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
    /// Dialogue character type enumeration
    /// </summary>
    public enum ChatRoleType
    {
        /// <summary>
        ///user
        /// </summary>
        User = 1,

        /// <summary>
        /// Assistant (AI)
        /// </summary>
        Assistant = 2
    }
}

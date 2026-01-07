using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel
{
    /// <summary>
    /// PromptResultChat：PromptResult 的对话历史记录
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PromptResultChat))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class PromptResultChat : EntityBase<int>
    {
        /// <summary>
        /// PromptResult 的 ID（外键）
        /// </summary>
        [Required]
        public int PromptResultId { get; private set; }

        /// <summary>
        /// 对话角色类型：User 或 Assistant
        /// </summary>
        [Required]
        public ChatRoleType RoleType { get; private set; }

        /// <summary>
        /// 对话内容
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// 对话顺序（在同一 PromptResult 中的顺序，从 1 开始）
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        /// 用户反馈：Like（true）、Unlike（false）、未反馈（null）
        /// </summary>
        public bool? UserFeedback { get; private set; }

        /// <summary>
        /// 用户评分（0-10分，可选）
        /// </summary>
        public decimal? UserScore { get; private set; }

        private PromptResultChat()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="promptResultId">PromptResult 的 ID</param>
        /// <param name="roleType">对话角色类型</param>
        /// <param name="content">对话内容</param>
        /// <param name="sequence">对话顺序</param>
        public PromptResultChat(int promptResultId, ChatRoleType roleType, string content, int sequence)
        {
            PromptResultId = promptResultId;
            RoleType = roleType;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Sequence = sequence;
        }

        /// <summary>
        /// 从 DTO 创建实体
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
        /// 更新用户反馈
        /// </summary>
        /// <param name="feedback">Like（true）、Unlike（false）、取消反馈（null）</param>
        /// <returns></returns>
        public PromptResultChat UpdateUserFeedback(bool? feedback)
        {
            UserFeedback = feedback;
            return this;
        }

        /// <summary>
        /// 更新用户评分
        /// </summary>
        /// <param name="score">评分（0-10分），null 表示取消评分</param>
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
    /// 对话角色类型枚举
    /// </summary>
    public enum ChatRoleType
    {
        /// <summary>
        /// 用户
        /// </summary>
        User = 1,

        /// <summary>
        /// 助手（AI）
        /// </summary>
        Assistant = 2
    }
}









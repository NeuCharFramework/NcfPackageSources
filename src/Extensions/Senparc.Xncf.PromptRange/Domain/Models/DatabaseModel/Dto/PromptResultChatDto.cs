using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using System;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    /// <summary>
    /// PromptResultChat DTO
    /// </summary>
    public class PromptResultChatDto : DtoBase
    {
        /// <summary>
        /// ID 主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// PromptResult 的 ID（外键）
        /// </summary>
        public int PromptResultId { get; set; }

        /// <summary>
        /// 对话角色类型：User 或 Assistant
        /// </summary>
        public ChatRoleType RoleType { get; set; }

        /// <summary>
        /// 对话内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 对话顺序（在同一 PromptResult 中的顺序，从 1 开始）
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 用户反馈：Like（true）、Unlike（false）、未反馈（null）
        /// </summary>
        public bool? UserFeedback { get; set; }

        /// <summary>
        /// 用户评分（0-10分，可选）
        /// </summary>
        public decimal? UserScore { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PromptResultChatDto()
        {
        }

        /// <summary>
        /// 从实体创建 DTO
        /// </summary>
        /// <param name="entity"></param>
        public PromptResultChatDto(PromptResultChat entity)
        {
            Id = entity.Id;
            PromptResultId = entity.PromptResultId;
            RoleType = entity.RoleType;
            Content = entity.Content;
            Sequence = entity.Sequence;
            UserFeedback = entity.UserFeedback;
            UserScore = entity.UserScore;
            AddTime = entity.AddTime;
        }
    }
}

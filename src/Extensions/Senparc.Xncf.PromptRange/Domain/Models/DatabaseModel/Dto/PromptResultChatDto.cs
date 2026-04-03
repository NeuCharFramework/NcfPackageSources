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
        ///ID primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///ID of PromptResult (foreign key)
        /// </summary>
        public int PromptResultId { get; set; }

        /// <summary>
        /// Dialogue role type: User or Assistant
        /// </summary>
        public ChatRoleType RoleType { get; set; }

        /// <summary>
        ///Conversation content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Conversation order (order within the same PromptResult, starting from 1)
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// User feedback: Like (true), Unlike (false), No feedback (null)
        /// </summary>
        public bool? UserFeedback { get; set; }

        /// <summary>
        /// User rating (0-10 points, optional)
        /// </summary>
        public decimal? UserScore { get; set; }

        /// <summary>
        ///Creation time
        /// </summary>
        public new DateTime AddTime { get; set; }

        /// <summary>
        ///Constructor
        /// </summary>
        public PromptResultChatDto()
        {
        }

        /// <summary>
        ///Create DTO from entity
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

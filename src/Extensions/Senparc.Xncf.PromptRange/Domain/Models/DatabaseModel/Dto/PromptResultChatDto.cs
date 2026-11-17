/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChatDto.cs
    File description: PromptResultChatDto related implementation

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
        ///Legacy comment translated to English.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int PromptResultId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public ChatRoleType RoleType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool? UserFeedback { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal? UserScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public new DateTime AddTime { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public PromptResultChatDto()
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
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

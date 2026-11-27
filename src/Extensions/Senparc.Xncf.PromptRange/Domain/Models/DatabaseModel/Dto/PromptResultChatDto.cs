/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChatDto.cs
    File description: PromptResultChatDto related implementation

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using System;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    /// <summary>
    /// Transfers one prompt-result chat message to application clients.
    /// </summary>
    public class PromptResultChatDto : DtoBase
    {
        /// <summary>Gets or sets the DTO identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the owning prompt-result identifier.</summary>
        public int PromptResultId { get; set; }

        /// <summary>Gets or sets the message role.</summary>
        public ChatRoleType RoleType { get; set; }

        /// <summary>Gets or sets the message content.</summary>
        public string Content { get; set; }

        /// <summary>Gets or sets the message sequence.</summary>
        public int Sequence { get; set; }

        /// <summary>Gets or sets user feedback.</summary>
        public bool? UserFeedback { get; set; }

        /// <summary>Gets or sets the user score.</summary>
        public decimal? UserScore { get; set; }

        /// <summary>Gets or sets the creation timestamp.</summary>
        public new DateTime AddTime { get; set; }

        /// <summary>Initializes an empty chat DTO.</summary>
        public PromptResultChatDto()
        {
        }

        /// <summary>Maps a prompt-result chat entity to this DTO.</summary>
        /// <param name="entity">The entity to map.</param>
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

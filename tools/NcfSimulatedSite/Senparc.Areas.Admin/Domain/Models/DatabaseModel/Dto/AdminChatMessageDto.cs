using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    ///AdminChatMessageDto: Management background chat message data transfer object
    /// </summary>
    public class AdminChatMessageDto : DtoBase<int>
    {
        /// <summary>
        /// belongs to the session ID
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// message role type
        /// </summary>
        public ChatMessageRoleType RoleType { get; set; }

        /// <summary>
        /// message content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// message sequence number
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// User feedback
        /// </summary>
        public MessageFeedbackType UserFeedback { get; set; }

        /// <summary>
        /// model identifier to use
        /// </summary>
        public string ModelIdentifier { get; set; }

        /// <summary>
        /// Mapping from entities to DTOs
        /// </summary>
        public static AdminChatMessageDto CreateFromEntity(AdminChatMessage entity)
        {
            if (entity == null) return null;

            return new AdminChatMessageDto
            {
                // Explicitly copy base class properties
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                // Copy business attributes
                SessionId = entity.SessionId,
                RoleType = entity.RoleType,
                Content = entity.Content,
                Sequence = entity.Sequence,
                UserFeedback = entity.UserFeedback,
                ModelIdentifier = entity.ModelIdentifier
            };
        }
    }

    /// <summary>
    /// Request to send chat message DTO
    /// </summary>
    public class ChatMessageInputDto
    {
        /// <summary>
        /// session id
        /// </summary>
        [Required]
        public int SessionId { get; set; }

        /// <summary>
        /// message content
        /// </summary>
        [Required]
        public string Content { get; set; }
    }
}

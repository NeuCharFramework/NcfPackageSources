using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    ///AdminChatSessionDto: Admin background chat session data transfer object
    /// </summary>
    public class AdminChatSessionDto : DtoBase<int>
    {
        /// <summary>
        /// session title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///userID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// session state
        /// </summary>
        public ChatSessionStatus Status { get; set; }

        /// <summary>
        ///Last message time
        /// </summary>
        public DateTime LastMessageTime { get; set; }

        /// <summary>
        /// Associated message list (optional, for nested queries)
        /// </summary>
        public List<AdminChatMessageDto> Messages { get; set; }

        /// <summary>
        /// List of associated modules (optional, for nested queries)
        /// </summary>
        public List<AdminChatSessionModuleDto> Modules { get; set; }

        public AdminChatSessionDto() 
        {
            Messages = new List<AdminChatMessageDto>();
            Modules = new List<AdminChatSessionModuleDto>();
        }

        /// <summary>
        /// Mapping from entities to DTOs
        /// </summary>
        public static AdminChatSessionDto CreateFromEntity(AdminChatSession entity)
        {
            if (entity == null) return null;

            return new AdminChatSessionDto
            {
                // Explicitly copy base class properties
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,

                // Copy business attributes
                Title = entity.Title,
                UserId = entity.UserId,
                Status = entity.Status,
                LastMessageTime = entity.LastMessageTime
            };
        }
    }

    /// <summary>
    /// Request DTO to create a chat session
    /// </summary>
    public class CreateChatSessionInputDto
    {
        /// <summary>
        ///Initial message content
        /// </summary>
        public string InitialMessage { get; set; }

        /// <summary>
        /// List of associated module UIDs
        /// </summary>
        public List<string> ModuleUids { get; set; }

        public CreateChatSessionInputDto()
        {
            ModuleUids = new List<string>();
        }
    }
}

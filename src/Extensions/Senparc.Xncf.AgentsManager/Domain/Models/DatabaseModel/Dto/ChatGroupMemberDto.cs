/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupMemberDto.cs
    文件功能描述：ChatGroupMemberDto 相关实现
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// ChatGroupMemer 数据库实体 DTO
    /// </summary>
    public class ChatGroupMemberDto : DtoBase
    {
        /// <summary>
        /// UID
        /// </summary>
        public string UID { get;  set; }

        /// <summary>
        /// AgentTemplateId
        /// </summary>
        public int AgentTemplateId { get;  set; }

        /// <summary>
        /// AgentTemplate（类型同名）
        /// </summary>
        public AgentTemplate AgentTemplate { get;  set; }

        /// <summary>
        /// chatgroupid
        /// </summary>
        public int ChatGroupId { get;  set; }

        ///// <summary>
        ///// ChatGroup（类型同名）
        ///// </summary>
        //public ChatGroup ChatGroup { get;  set; }

        public ChatGroupMemberDto() { }

        public ChatGroupMemberDto(string uID, int chatGroupId, int agentTemplateId /*,AgentTemplate agentTemplate  ChatGroup chatGroup*/)
        {
            UID = uID;
            ChatGroupId = chatGroupId;
            AgentTemplateId = agentTemplateId;
            //AgentTemplate = agentTemplate;
            //ChatGroup = chatGroup;
        }

        public ChatGroupMemberDto(ChatGroupMember chatGroupMemer)
        {
            UID = chatGroupMemer.UID;
            AgentTemplateId = chatGroupMemer.AgentTemplateId;
            AgentTemplate = chatGroupMemer.AgentTemplate;
            ChatGroupId = chatGroupMemer.ChatGroupId;
            //ChatGroup = chatGroupMemer.ChatGroup;
        }
    }
}

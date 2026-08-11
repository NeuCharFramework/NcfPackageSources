using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// ChatGroup 的远程 A2A 成员。
    ///
    /// 与 ChatGroupMember 分表保存，避免修改已运行本地 Agent 使用的必填 AgentTemplateId 外键。
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(ChatGroupRemoteMember))]
    [Serializable]
    public class ChatGroupRemoteMember : EntityBase<int>
    {
        [Required]
        public string UID { get; private set; }

        [Required]
        public int ChatGroupId { get; private set; }

        [Required]
        [ForeignKey(nameof(RemoteAgent))]
        public int RemoteAgentId { get; private set; }

        public RemoteAgent RemoteAgent { get; private set; }

        [Required]
        public bool Enable { get; private set; }

        /// <summary>null 时继承 ChatGroup 的 ContextSharingMode。</summary>
        public ChatGroupContextSharingMode? ContextSharingMode { get; private set; }

        private ChatGroupRemoteMember() { }

        public ChatGroupRemoteMember(ChatGroupRemoteMemberDto dto)
        {
            UID = string.IsNullOrWhiteSpace(dto.UID) ? Guid.NewGuid().ToString("N") : dto.UID;
            ChatGroupId = dto.ChatGroupId;
            RemoteAgentId = dto.RemoteAgentId;
            Enable = dto.Enable;
            ContextSharingMode = dto.ContextSharingMode;
        }

        public void Update(ChatGroupRemoteMemberDto dto)
        {
            Enable = dto.Enable;
            ContextSharingMode = dto.ContextSharingMode;
        }
    }
}

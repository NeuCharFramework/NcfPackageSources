/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RemoteAgentDto.cs
    文件功能描述：数据模型、DTO 与映射定义


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    public class RemoteAgentDto : DtoBase<int>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enable { get; set; } = true;
        public RemoteAgentProtocol Protocol { get; set; } = RemoteAgentProtocol.A2A;
        public string AgentCardUrl { get; set; }
        public RemoteAgentAuthenticationMode AuthenticationMode { get; set; }
        public string AuthHeaderName { get; set; }
        public string AuthSecretKey { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
        public RemoteAgentConnectionStatus ConnectionStatus { get; set; }
        public System.DateTime? LastHealthCheckAt { get; set; }
        public string LastHealthCheckMessage { get; set; }

        public RemoteAgentDto() { }

        public RemoteAgentDto(RemoteAgent remoteAgent)
        {
            Id = remoteAgent.Id;
            Name = remoteAgent.Name;
            Description = remoteAgent.Description;
            Enable = remoteAgent.Enable;
            Protocol = remoteAgent.Protocol;
            AgentCardUrl = remoteAgent.AgentCardUrl;
            AuthenticationMode = remoteAgent.AuthenticationMode;
            AuthHeaderName = remoteAgent.AuthHeaderName;
            AuthSecretKey = remoteAgent.AuthSecretKey;
            TimeoutSeconds = remoteAgent.TimeoutSeconds;
            ConnectionStatus = remoteAgent.ConnectionStatus;
            LastHealthCheckAt = remoteAgent.LastHealthCheckAt;
            LastHealthCheckMessage = remoteAgent.LastHealthCheckMessage;
        }
    }

    public class ChatGroupRemoteMemberDto : DtoBase<int>
    {
        public string UID { get; set; }
        public int ChatGroupId { get; set; }
        public int RemoteAgentId { get; set; }
        public bool Enable { get; set; } = true;
        public ChatGroupContextSharingMode? ContextSharingMode { get; set; }
        public RemoteAgentDto RemoteAgentDto { get; set; }

        public ChatGroupRemoteMemberDto() { }

        public ChatGroupRemoteMemberDto(ChatGroupRemoteMember member)
        {
            Id = member.Id;
            UID = member.UID;
            ChatGroupId = member.ChatGroupId;
            RemoteAgentId = member.RemoteAgentId;
            Enable = member.Enable;
            ContextSharingMode = member.ContextSharingMode;
            RemoteAgentDto = member.RemoteAgent == null ? null : new RemoteAgentDto(member.RemoteAgent);
        }
    }
}

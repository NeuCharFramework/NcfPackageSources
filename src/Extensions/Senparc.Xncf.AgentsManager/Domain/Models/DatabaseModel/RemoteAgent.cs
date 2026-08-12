/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RemoteAgent.cs
    文件功能描述：数据模型、DTO 与映射定义


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// 远程智能体协议类型。第一阶段只开放 A2A，预留该枚举便于后续扩展。
    /// </summary>
    public enum RemoteAgentProtocol
    {
        A2A = 0
    }

    /// <summary>
    /// 远程服务鉴权方式。数据库只保存密钥引用，不保存令牌正文。
    /// </summary>
    public enum RemoteAgentAuthenticationMode
    {
        None = 0,
        BearerToken = 1,
        CustomHeader = 2
    }

    /// <summary>
    /// 群聊对参与者可见的上下文范围。
    /// </summary>
    public enum ChatGroupContextSharingMode
    {
        /// <summary>兼容旧群组：保持当前工作流历史行为。</summary>
        LegacyFullHistory = 0,
        /// <summary>任务指令和已完成轮次的简短结论。</summary>
        InstructionAndKeyReplies = 1,
        /// <summary>仅任务指令和当前轮需要的最小上下文。</summary>
        InstructionOnly = 2
    }

    /// <summary>
    /// 远程 Agent 的连接状态，仅用于管理与诊断，不作为运行的唯一判断依据。
    /// </summary>
    public enum RemoteAgentConnectionStatus
    {
        Unknown = 0,
        Available = 1,
        Unavailable = 2
    }

    /// <summary>
    /// A2A 远程智能体配置。访问令牌在部署配置 A2A:Secrets:{AuthSecretKey} 中维护。
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(RemoteAgent))]
    [Serializable]
    public class RemoteAgent : EntityBase<int>
    {
        [Required]
        public string Name { get; private set; }

        public string Description { get; private set; }

        [Required]
        public bool Enable { get; private set; }

        [Required]
        public RemoteAgentProtocol Protocol { get; private set; }

        /// <summary>
        /// A2A 服务根地址，或标准 /.well-known/agent-card.json 地址。
        /// </summary>
        [Required]
        public string AgentCardUrl { get; private set; }

        public RemoteAgentAuthenticationMode AuthenticationMode { get; private set; }

        /// <summary>CustomHeader 模式的请求头名称。</summary>
        public string AuthHeaderName { get; private set; }

        /// <summary>部署配置中的密钥名，而不是访问令牌。</summary>
        public string AuthSecretKey { get; private set; }

        public int TimeoutSeconds { get; private set; }

        public RemoteAgentConnectionStatus ConnectionStatus { get; private set; }

        public DateTime? LastHealthCheckAt { get; private set; }

        public string LastHealthCheckMessage { get; private set; }

        public ICollection<ChatGroupRemoteMember> ChatGroupRemoteMembers { get; private set; }

        private RemoteAgent() { }

        public RemoteAgent(RemoteAgentDto dto)
        {
            Update(dto);
        }

        public void Update(RemoteAgentDto dto)
        {
            Name = dto.Name?.Trim();
            Description = dto.Description;
            Enable = dto.Enable;
            Protocol = dto.Protocol;
            AgentCardUrl = dto.AgentCardUrl?.Trim();
            AuthenticationMode = dto.AuthenticationMode;
            AuthHeaderName = dto.AuthHeaderName?.Trim();
            AuthSecretKey = dto.AuthSecretKey?.Trim();
            TimeoutSeconds = dto.TimeoutSeconds <= 0 ? 60 : Math.Min(dto.TimeoutSeconds, 600);
        }

        public void SetConnectionStatus(RemoteAgentConnectionStatus status, string message)
        {
            ConnectionStatus = status;
            LastHealthCheckAt = DateTime.Now;
            LastHealthCheckMessage = message;
        }

        public void EnableAgent() => Enable = true;

        public void DisableAgent() => Enable = false;
    }
}

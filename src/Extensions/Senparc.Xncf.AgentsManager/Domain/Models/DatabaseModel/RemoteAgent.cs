/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteAgent.cs
    File description: data model, DTO, and mapping definitions

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

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
    /// Identifies the protocol used to communicate with a remote agent.
    /// </summary>
    public enum RemoteAgentProtocol
    {
        A2A = 0
    }

    /// <summary>
    /// Identifies how credentials are supplied to a remote agent.
    /// </summary>
    public enum RemoteAgentAuthenticationMode
    {
        None = 0,
        BearerToken = 1,
        CustomHeader = 2
    }

    /// <summary>
    /// Controls how much conversation context is shared with a group member.
    /// </summary>
    public enum ChatGroupContextSharingMode
    {
        /// Reuses the complete prior conversation history.
        LegacyFullHistory = 0,
        /// Shares instructions and selected key replies.
        InstructionAndKeyReplies = 1,
        /// Shares only the configured instructions.
        InstructionOnly = 2
    }

    /// <summary>
    /// Indicates the most recent health-check state of a remote agent.
    /// </summary>
    public enum RemoteAgentConnectionStatus
    {
        Unknown = 0,
        Available = 1,
        Unavailable = 2
    }

    /// <summary>
    /// Stores a persisted remote agent configuration and health status.
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

        /// <summary>Gets the remote Agent card URL used for discovery.</summary>
        [Required]
        public string AgentCardUrl { get; private set; }

        public RemoteAgentAuthenticationMode AuthenticationMode { get; private set; }

        /// <summary>Gets the custom authentication header name.</summary>
        public string AuthHeaderName { get; private set; }

        /// <summary>Gets the protected authentication secret.</summary>
        public string AuthSecretKey { get; private set; }

        public int TimeoutSeconds { get; private set; }

        public RemoteAgentConnectionStatus ConnectionStatus { get; private set; }

        public DateTime? LastHealthCheckAt { get; private set; }

        public string LastHealthCheckMessage { get; private set; }

        public ICollection<ChatGroupRemoteMember> ChatGroupRemoteMembers { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private RemoteAgent() { }

        /// <summary>
        /// Initializes a remote agent from a configuration DTO.
        /// </summary>
        /// <param name="dto">The remote-agent configuration.</param>
        public RemoteAgent(RemoteAgentDto dto)
        {
            Update(dto);
        }

        /// <summary>
        /// Updates connection settings and clamps the configured request timeout.
        /// </summary>
        /// <param name="dto">The replacement remote-agent configuration.</param>
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

        /// <summary>
        /// Records the latest health-check status and diagnostic message.
        /// </summary>
        /// <param name="status">The health-check result.</param>
        /// <param name="message">The provider diagnostic message.</param>
        public void SetConnectionStatus(RemoteAgentConnectionStatus status, string message)
        {
            ConnectionStatus = status;
            LastHealthCheckAt = DateTime.Now;
            LastHealthCheckMessage = message;
        }

        /// Enables this remote agent for selection and invocation.
        public void EnableAgent() => Enable = true;

        /// Disables this remote agent without deleting its configuration.
        public void DisableAgent() => Enable = false;
    }
}

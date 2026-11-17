/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteAgent.cs
    File description: data model, DTO, and mapping definitions

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

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
    ///Legacy comment translated to English.
    /// </summary>
    public enum RemoteAgentProtocol
    {
        A2A = 0
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum RemoteAgentAuthenticationMode
    {
        None = 0,
        BearerToken = 1,
        CustomHeader = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ChatGroupContextSharingMode
    {
        ///Legacy comment translated to English.
        LegacyFullHistory = 0,
        ///Legacy comment translated to English.
        InstructionAndKeyReplies = 1,
        ///Legacy comment translated to English.
        InstructionOnly = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum RemoteAgentConnectionStatus
    {
        Unknown = 0,
        Available = 1,
        Unavailable = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string AgentCardUrl { get; private set; }

        public RemoteAgentAuthenticationMode AuthenticationMode { get; private set; }

        ///Legacy comment translated to English.
        public string AuthHeaderName { get; private set; }

        ///Legacy comment translated to English.
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

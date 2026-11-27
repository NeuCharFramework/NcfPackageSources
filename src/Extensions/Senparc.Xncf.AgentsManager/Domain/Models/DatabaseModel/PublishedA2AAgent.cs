/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2AAgent.cs
    File description: data model, DTO, and mapping definitions

    Created by: Senparc - 20260813

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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// Stores the public A2A publication settings for an agent template.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PublishedA2AAgent))]
    [Serializable]
    public class PublishedA2AAgent : EntityBase<int>
    {
        private static readonly Regex PublicAgentKeyPattern = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [Required]
        public int AgentTemplateId { get; private set; }

        /// <summary>
        /// Gets or sets the public identifier used in the A2A route.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string PublicAgentKey { get; private set; }

        /// Gets or sets whether the published agent is enabled.
        [Required]
        public bool Enable { get; private set; }

        /// Gets or sets the public agent card name.
        [MaxLength(120)]
        public string CardName { get; private set; }

        /// Gets or sets the public agent card description.
        [MaxLength(1000)]
        public string CardDescription { get; private set; }

        [MaxLength(64)]
        public string SkillId { get; private set; }

        [MaxLength(120)]
        public string SkillName { get; private set; }

        [MaxLength(1000)]
        public string SkillDescription { get; private set; }

        /// <summary>
        /// Gets or sets whether function calls are permitted.
        /// </summary>
        [Required]
        public bool AllowFunctionCalls { get; private set; }

        /// Gets or sets the maximum accepted input character count.
        [Required]
        public int MaxInputCharacters { get; private set; }

        /// Gets or sets the authentication mode used by the published endpoint.
        [Required]
        public RemoteAgentAuthenticationMode AuthenticationMode { get; private set; }

        /// <summary>Gets or sets the authentication header name.</summary>
        [MaxLength(100)]
        public string AuthHeaderName { get; private set; }

        /// <summary>Gets or sets the protected authentication secret.</summary>
        [MaxLength(100)]
        public string AuthSecretKey { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private PublishedA2AAgent() { }

        /// <summary>
        /// Initializes publication settings from a DTO.
        /// </summary>
        /// <param name="dto">The publication settings to copy.</param>
        public PublishedA2AAgent(PublishedA2AAgentDto dto)
        {
            Update(dto);
        }

        /// <summary>
        /// Replaces publication settings and validates the public key and input limits.
        /// </summary>
        /// <param name="dto">The replacement publication settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the public agent key is invalid.</exception>
        public void Update(PublishedA2AAgentDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            AgentTemplateId = dto.AgentTemplateId;
            PublicAgentKey = NormalizePublicAgentKey(dto.PublicAgentKey);
            Enable = dto.Enable;
            CardName = dto.CardName?.Trim();
            CardDescription = dto.CardDescription?.Trim();
            SkillId = string.IsNullOrWhiteSpace(dto.SkillId) ? "chat" : dto.SkillId.Trim();
            SkillName = dto.SkillName?.Trim();
            SkillDescription = dto.SkillDescription?.Trim();
            AllowFunctionCalls = dto.AllowFunctionCalls;
            MaxInputCharacters = Math.Clamp(dto.MaxInputCharacters <= 0 ? 12000 : dto.MaxInputCharacters, 512, 100000);
            AuthenticationMode = dto.AuthenticationMode;
            AuthHeaderName = dto.AuthHeaderName?.Trim();
            AuthSecretKey = dto.AuthSecretKey?.Trim();
        }

        /// <summary>Enables this published agent.</summary>
        public void EnableAgent() => Enable = true;

        /// <summary>Disables this published agent without deleting its publication settings.</summary>
        public void DisableAgent() => Enable = false;

        /// <summary>
        /// Normalizes and validates a public A2A agent key.
        /// </summary>
        /// <param name="publicAgentKey">The key to normalize.</param>
        /// <returns>The lower-case validated key.</returns>
        /// <exception cref="ArgumentException">Thrown when the key contains unsupported characters or length.</exception>
        public static string NormalizePublicAgentKey(string publicAgentKey)
        {
            var normalized = publicAgentKey?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized) || !PublicAgentKeyPattern.IsMatch(normalized))
            {
                throw new ArgumentException("A2A 公开标识只能使用小写字母、数字和连字符，长度为 1-64 位。", nameof(publicAgentKey));
            }

            return normalized;
        }
    }
}

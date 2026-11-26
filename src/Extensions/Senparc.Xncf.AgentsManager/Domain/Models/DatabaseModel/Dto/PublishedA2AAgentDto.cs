/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2AAgentDto.cs
    File description: data model, DTO, and mapping definitions

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// Transfers the public A2A-agent publication, skill, and authentication settings.
    /// </summary>
    public class PublishedA2AAgentDto : DtoBase<int>
    {
        public int AgentTemplateId { get; set; }
        public string PublicAgentKey { get; set; }
        public bool Enable { get; set; }
        public string CardName { get; set; }
        public string CardDescription { get; set; }
        public string SkillId { get; set; } = "chat";
        public string SkillName { get; set; }
        public string SkillDescription { get; set; }
        public bool AllowFunctionCalls { get; set; }
        public int MaxInputCharacters { get; set; } = 12000;
        public RemoteAgentAuthenticationMode AuthenticationMode { get; set; }
        public string AuthHeaderName { get; set; }
        public string AuthSecretKey { get; set; }

        /// Gets or sets the generated public Agent-card URL.
        public string AgentCardUrl { get; set; }

        /// <summary>
        /// Initializes an empty publication DTO for binding.
        /// </summary>
        public PublishedA2AAgentDto() { }

        /// <summary>
        /// Projects a persisted A2A publication entity into a transport DTO.
        /// </summary>
        /// <param name="publishedAgent">The publication entity to project.</param>
        public PublishedA2AAgentDto(PublishedA2AAgent publishedAgent)
        {
            Id = publishedAgent.Id;
            AgentTemplateId = publishedAgent.AgentTemplateId;
            PublicAgentKey = publishedAgent.PublicAgentKey;
            Enable = publishedAgent.Enable;
            CardName = publishedAgent.CardName;
            CardDescription = publishedAgent.CardDescription;
            SkillId = publishedAgent.SkillId;
            SkillName = publishedAgent.SkillName;
            SkillDescription = publishedAgent.SkillDescription;
            AllowFunctionCalls = publishedAgent.AllowFunctionCalls;
            MaxInputCharacters = publishedAgent.MaxInputCharacters;
            AuthenticationMode = publishedAgent.AuthenticationMode;
            AuthHeaderName = publishedAgent.AuthHeaderName;
            AuthSecretKey = publishedAgent.AuthSecretKey;
        }
    }
}

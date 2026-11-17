/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PublishedA2AAgentDto.cs
    File description: data model, DTO, and mapping definitions


    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    ///Legacy comment translated to English.
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

        ///Legacy comment translated to English.
        public string AgentCardUrl { get; set; }

        public PublishedA2AAgentDto() { }

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

using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto
{
    /// <summary>
    /// 本地 Agent 的 A2A 对外发布配置。AuthSecretKey 仅为部署配置键名。
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

        /// <summary>当前请求下可直接使用的标准发现地址，仅由接口响应填充。</summary>
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

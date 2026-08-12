using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models
{
    /// <summary>
    /// 本地 AgentTemplate 对外发布为标准 A2A Agent 的配置。
    /// 与 AgentTemplate 解耦，确保现有本地 Agent 保持原有行为。
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(PublishedA2AAgent))]
    [Serializable]
    public class PublishedA2AAgent : EntityBase<int>
    {
        private static readonly Regex PublicAgentKeyPattern = new("^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [Required]
        public int AgentTemplateId { get; private set; }

        /// <summary>
        /// 对外稳定标识，同时组成 A2A URL。仅允许小写字母、数字和连字符。
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string PublicAgentKey { get; private set; }

        /// <summary>发布开关。即使本地 Agent 仍启用，关闭此项也不会接收外部 A2A 请求。</summary>
        [Required]
        public bool Enable { get; private set; }

        /// <summary>可选的 Agent Card 名称；留空时使用本地 Agent 名称。</summary>
        [MaxLength(120)]
        public string CardName { get; private set; }

        /// <summary>可选的 Agent Card 公开描述。不得填写 Prompt、密钥或内部工具信息。</summary>
        [MaxLength(1000)]
        public string CardDescription { get; private set; }

        [MaxLength(64)]
        public string SkillId { get; private set; }

        [MaxLength(120)]
        public string SkillName { get; private set; }

        [MaxLength(1000)]
        public string SkillDescription { get; private set; }

        /// <summary>
        /// 默认关闭。开启后，远程输入可能触发本地 Function Calling 或 MCP 工具，须由管理员显式确认。
        /// </summary>
        [Required]
        public bool AllowFunctionCalls { get; private set; }

        /// <summary>单次可发送给本地模型的最大文本字符数。</summary>
        [Required]
        public int MaxInputCharacters { get; private set; }

        /// <summary>入站鉴权方式。实际密钥只保存在 A2A:InboundSecrets:{AuthSecretKey}。</summary>
        [Required]
        public RemoteAgentAuthenticationMode AuthenticationMode { get; private set; }

        /// <summary>CustomHeader 入站鉴权的请求头名称。</summary>
        [MaxLength(100)]
        public string AuthHeaderName { get; private set; }

        /// <summary>部署配置中的密钥名，不保存令牌正文。</summary>
        [MaxLength(100)]
        public string AuthSecretKey { get; private set; }

        private PublishedA2AAgent() { }

        public PublishedA2AAgent(PublishedA2AAgentDto dto)
        {
            Update(dto);
        }

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

        public void EnableAgent() => Enable = true;

        public void DisableAgent() => Enable = false;

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

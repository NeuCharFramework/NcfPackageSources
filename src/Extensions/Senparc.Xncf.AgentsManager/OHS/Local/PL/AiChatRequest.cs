using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    /// <summary>
    /// AI 对话消息记录
    /// </summary>
    public class AiChatMessageDto
    {
        /// <summary>
        /// 角色：user / assistant
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// 发送 AI Chat 消息的请求
    /// </summary>
    public class AiChat_SendMessageRequest
    {
        /// <summary>
        /// 用户输入的消息
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// 历史对话记录
        /// </summary>
        public List<AiChatMessageDto> ChatHistory { get; set; } = new List<AiChatMessageDto>();

        /// <summary>
        /// 使用的 AI 模型 ID（0 表示使用默认配置）
        /// </summary>
        public int AiModelId { get; set; } = 0;

        /// <summary>
        /// 待确认的 ChatGroup 建议信息（在用户确认后自动创建并运行）
        /// </summary>
        public AiChat_SuggestedGroupDto PendingGroup { get; set; }
    }

    /// <summary>
    /// AI 建议创建的 ChatGroup 信息
    /// </summary>
    public class AiChat_SuggestedGroupDto
    {
        /// <summary>
        /// 建议的组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 组说明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 群主 AgentTemplate ID
        /// </summary>
        public int AdminAgentTemplateId { get; set; }

        /// <summary>
        /// 对接人 AgentTemplate ID
        /// </summary>
        public int EnterAgentTemplateId { get; set; }

        /// <summary>
        /// 成员 AgentTemplate ID 列表
        /// </summary>
        public List<int> MemberAgentTemplateIds { get; set; } = new List<int>();

        /// <summary>
        /// 用户的任务命令（用于运行时传入）
        /// </summary>
        public string TaskCommand { get; set; }

        /// <summary>
        /// 使用的 AI 模型 ID
        /// </summary>
        public int AiModelId { get; set; }
    }

    /// <summary>
    /// 运行已有 ChatGroup 的请求
    /// </summary>
    public class AiChat_RunExistingGroupRequest
    {
        /// <summary>
        /// ChatGroup ID
        /// </summary>
        public int ChatGroupId { get; set; }

        /// <summary>
        /// 任务命令
        /// </summary>
        public string TaskCommand { get; set; }

        /// <summary>
        /// AI 模型 ID（0 表示使用默认配置）
        /// </summary>
        public int AiModelId { get; set; } = 0;
    }
}

using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    /// <summary>
    /// AI Chat 消息类型
    /// </summary>
    public enum AiChatResponseType
    {
        /// <summary>
        /// 普通回复
        /// </summary>
        Message = 0,

        /// <summary>
        /// 建议创建新组（等待用户确认）
        /// </summary>
        SuggestCreateGroup = 1,

        /// <summary>
        /// 已找到合适的组，建议直接运行任务
        /// </summary>
        SuggestRunTask = 2,

        /// <summary>
        /// 已开始创建并运行任务
        /// </summary>
        TaskStarted = 3,

        /// <summary>
        /// 错误
        /// </summary>
        Error = 4
    }

    /// <summary>
    /// AI Chat 发送消息的响应
    /// </summary>
    public class AiChat_SendMessageResponse
    {
        /// <summary>
        /// AI 回复内容（用于展示给用户）
        /// </summary>
        public string AiMessage { get; set; }

        /// <summary>
        /// 响应类型
        /// </summary>
        public AiChatResponseType ResponseType { get; set; }

        /// <summary>
        /// 建议创建的 ChatGroup 信息（当 ResponseType 为 SuggestCreateGroup 时有值）
        /// </summary>
        public AiChat_SuggestedGroupDto SuggestedGroup { get; set; }

        /// <summary>
        /// 建议运行的 ChatGroup ID（当 ResponseType 为 SuggestRunTask 时有值）
        /// </summary>
        public int? SuggestedGroupId { get; set; }

        /// <summary>
        /// 已启动的任务所属 ChatGroup ID（当 ResponseType 为 TaskStarted 时有值）
        /// </summary>
        public int? StartedGroupId { get; set; }

        /// <summary>
        /// 已启动任务的名称
        /// </summary>
        public string StartedTaskName { get; set; }
    }
}

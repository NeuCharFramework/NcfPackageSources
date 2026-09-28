/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupRunCommand.cs
    文件功能描述：ChatGroup 执行的领域层命令对象
    
    创建标识：Senparc - 20260927
    创建描述：将 ChatGroupService 的执行入口与 Application 层 HTTP/UI 绑定模型（ChatGroup_RunGroupRequest）解耦

----------------------------------------------------------------*/

using System.Threading;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// ChatGroup 执行领域命令。Domain 层执行入口（RunChatGroupInThread / RunChatGroupAwaitAsync /
/// RunChatGroupAwaitWithResultAsync）统一接收该类型，Application 层负责把
/// ChatGroup_RunGroupRequest 等绑定模型映射为命令。
/// </summary>
public sealed class ChatGroupRunCommand
{
    /// <summary>
    /// 任务名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ChatGroup ID
    /// </summary>
    public int ChatGroupId { get; set; }

    /// <summary>
    /// 如果是 0 ，则使用系统默认配置
    /// </summary>
    public int AiModelId { get; set; }

    /// <summary>
    /// 发起对话的要求
    /// </summary>
    public string PromptCommand { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 使用个性化智能体
    /// </summary>
    public bool Personality { get; set; }

    /// <summary>
    /// 消息平台
    /// </summary>
    public HookPlatform HookPlatform { get; set; }

    /// <summary>
    /// 消息平台参数
    /// </summary>
    public string HookParameter { get; set; }

    /// <summary>
    /// 最大对话轮数
    /// </summary>
    public int ChatMaxRound { get; set; } = ChatGroupService.DefaultChatMaxRound;

    /// <summary>
    /// 可选：业务关联 ID（例如 Prompt 优化的 RequestId），用于在执行上下文中关联工具调用
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// 是否要求工具调用先取得人工批准。默认关闭；启用后任务会在工具调用处暂停，
    /// 由 Human-in-the-Loop API/SSE 完成批准或拒绝后继续执行。
    /// </summary>
    public bool RequireHumanApproval { get; set; }

    /// <summary>HIL 等级：0 自动，1 风险分层，2 工具审批，3 Human 参与者。</summary>
    public HumanInTheLoopLevel HumanInTheLoopLevel { get; set; } = HumanInTheLoopLevel.Automatic;

    /// <summary>插件工具权限覆盖；Inherit 表示按 HIL 等级计算。</summary>
    public ToolPermissionMode PluginToolPermission { get; set; } = ToolPermissionMode.Inherit;

    /// <summary>MCP 工具权限覆盖；Inherit 表示按 HIL 等级计算。</summary>
    public ToolPermissionMode McpToolPermission { get; set; } = ToolPermissionMode.Inherit;

    /// <summary>是否在本次 Group 执行中包含 Human 参与者。</summary>
    public bool IncludeHumanParticipant { get; set; }

    /// <summary>启动任务时捕获的提醒接收人，不参与 HTTP 模型绑定。</summary>
    public string HumanRecipientUserId { get; set; }

    /// <summary>
    /// 可选：由 Workflow 或宿主传入的取消信号，用于进程内调用以便及时终止外部 Agent 资源。
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}

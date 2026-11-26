/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.ChatTool.cs
    File description: AdminChat command registration and chat tool services

    Created by: Senparc - 20261125

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.AgentsManager.Application.ChatTools;
using Senparc.Xncf.AgentsManager.OHS.Local.AppService;
using System.Collections.Generic;

namespace Senparc.Xncf.AgentsManager;

public partial class Register : IChatTool
{
    /// <summary>
    /// Returns the AdminChat command definition for running an existing Agent group.
    /// </summary>
    /// <returns>The registered Agent group chat-tool definitions.</returns>
    public IReadOnlyCollection<ChatToolDefinition> GetChatTools()
    {
        return new[]
        {
            new ChatToolDefinition
            {
                Command = "/run-agent-group",
                Label = AgentsManagerResource.Get(
                    "AdminChat.Command.RunAgentGroup",
                    "Run an Agent group"),
                Hint = AgentsManagerResource.Get(
                    "AdminChat.Command.RunAgentGroupHint",
                    "Describe the task; reuse an existing matching group"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Parameters = new[]
                {
                    new ChatToolParameterDefinition
                    {
                        Name = "group",
                        Label = AgentsManagerResource.Get(
                            "AdminChat.Command.RunAgentGroup.Parameter.Group",
                            "Agent group"),
                        Hint = AgentsManagerResource.Get(
                            "AdminChat.Command.RunAgentGroup.Parameter.GroupHint",
                            "Choose an enabled Agent group"),
                        Placeholder = "ChatGroupID",
                        Prefix = "--group=",
                        OptionsProviderType = typeof(ChatGroupChatToolParameterOptionsProvider)
                    },
                    new ChatToolParameterDefinition
                    {
                        Name = "task",
                        Label = AgentsManagerResource.Get(
                            "AdminChat.Command.RunAgentGroup.Parameter.Task",
                            "Task"),
                        Hint = AgentsManagerResource.Get(
                            "AdminChat.Command.RunAgentGroup.Parameter.TaskHint",
                            "Describe the task to run"),
                        Placeholder = AgentsManagerResource.Get(
                            "AdminChat.Command.RunAgentGroup.Parameter.TaskPlaceholder",
                            "Enter the task description"),
                        IsPositional = true,
                        Required = true,
                        Order = 2
                    }
                },
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(ChatGroupAppService),
                        MethodName = nameof(ChatGroupAppService.FindChatGroups)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(ChatGroupAppService),
                        MethodName = nameof(ChatGroupAppService.StartChatToolTask)
                    }
                },
                Instructions = """

[Agent Group Command]
The user asked to run an existing AgentsManager ChatGroup. Do not create, edit, or delete a group.
1. If --group contains an ID, do not use FindChatGroups to rediscover it. Pass that exact numeric ID as ChatGroupId to StartChatToolTask.
2. Use FindChatGroups only when the user supplied a name or task description without an explicit ID. Choose only when one group clearly matches; otherwise show the closest options and ask.
3. Pass the remaining user text as Command. Even a short phrase such as "执行任务" is a valid command and must not be rejected as missing.
4. StartChatToolTask returns after a real ChatTask is created. Treat the ChatTask as running asynchronously and show its preview/action metadata; do not describe it as completed.
5. If the function response has Success=false, quote its ErrorMessage exactly. Never replace a concrete startup error with a generic failure statement.
"""
            }
        };
    }

    /// <summary>
    /// Registers the parameter-options provider used by the Agent group chat command.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    private static void AddChatToolServices(IServiceCollection services)
    {
        services.AddScoped<ChatGroupChatToolParameterOptionsProvider>();
        services.AddScoped<IChatToolAsyncActionMonitor, AgentTaskChatToolAsyncActionMonitor>();
    }
}

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
                        MethodName = nameof(ChatGroupAppService.RunChatGroup)
                    }
                },
                Instructions = """

[Agent Group Command]
The user asked to run an existing AgentsManager ChatGroup. Do not create, edit, or delete a group.
1. Search enabled groups with FindChatGroups using task keywords, group name, or the explicit --group value.
2. Reuse an exact ID when supplied. For a semantic match, choose only when one group clearly matches; otherwise show the closest options and ask the user to choose.
3. Start the selected existing group with RunChatGroup and the user's task as Command.
4. Report the selected group and the resulting task status; do not claim completion because the run is asynchronous.
"""
            }
        };
    }

    private static void AddChatToolServices(IServiceCollection services)
    {
        services.AddScoped<ChatGroupChatToolParameterOptionsProvider>();
    }
}

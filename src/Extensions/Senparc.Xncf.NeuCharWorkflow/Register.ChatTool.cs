/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.ChatTool.cs
    File description: AdminChat command registration and chat tool services

    Created by: Senparc - 20261125

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.NeuCharWorkflow.Application.ChatTools;
using Senparc.Xncf.NeuCharWorkflow.OHS.Local.AppService;
using System.Collections.Generic;

namespace Senparc.Xncf.NeuCharWorkflow;

public partial class Register : IChatTool
{
    public IReadOnlyCollection<ChatToolDefinition> GetChatTools()
    {
        return new[]
        {
            new ChatToolDefinition
            {
                Command = "/run-workflow",
                Label = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.RunWorkflow",
                    "Run a Workflow"),
                Hint = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.RunWorkflowHint",
                    "Describe the task or provide --id=<workflowId>"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Parameters = new[]
                {
                    new ChatToolParameterDefinition
                    {
                        Name = "id",
                        Label = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.Workflow.Parameter.Id",
                            "Workflow"),
                        Hint = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.Workflow.Parameter.IdHint",
                            "Choose a workflow owned by the current administrator"),
                        Placeholder = "workflowId",
                        Prefix = "--id=",
                        OptionsProviderType = typeof(WorkflowChatToolParameterOptionsProvider)
                    },
                    new ChatToolParameterDefinition
                    {
                        Name = "input",
                        Label = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.RunWorkflow.Parameter.Input",
                            "Workflow input"),
                        Hint = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.RunWorkflow.Parameter.InputHint",
                            "Enter optional input for this run"),
                        Placeholder = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.RunWorkflow.Parameter.InputPlaceholder",
                            "Describe the input or task"),
                        IsPositional = true,
                        Order = 2
                    }
                },
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.SearchWorkflows)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.StartWorkflow)
                    }
                },
                Instructions = """

[Workflow Run Command]
The user asked to start an existing NeuChar Workflow owned by the current administrator.
1. Parse an explicit --id value or use SearchWorkflows to find a semantic match by name and description.
2. If more than one candidate is plausible, show the candidates and ask which one to use.
3. Pass the user's remaining text as workflow input and call StartWorkflow exactly once.
4. Report the workflow name and run ID; the workflow runs asynchronously, so do not claim it completed.
5. Do not create, edit, delete, or import a workflow in this command.
"""
            },
            new ChatToolDefinition
            {
                Command = "/edit-workflow",
                Label = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.EditWorkflow",
                    "Assist with Workflow editing"),
                Hint = NeuCharWorkflowResource.Get(
                    "AdminChat.Command.EditWorkflowHint",
                    "Provide --id=<workflowId> and describe the change"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Parameters = new[]
                {
                    new ChatToolParameterDefinition
                    {
                        Name = "id",
                        Label = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.Workflow.Parameter.Id",
                            "Workflow"),
                        Hint = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.Workflow.Parameter.IdHint",
                            "Choose a workflow owned by the current administrator"),
                        Placeholder = "workflowId",
                        Prefix = "--id=",
                        Required = true,
                        OptionsProviderType = typeof(WorkflowChatToolParameterOptionsProvider)
                    },
                    new ChatToolParameterDefinition
                    {
                        Name = "revision",
                        Label = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.EditWorkflow.Parameter.Revision",
                            "Revision"),
                        Hint = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.EditWorkflow.Parameter.RevisionHint",
                            "Optional expected revision for optimistic concurrency"),
                        Placeholder = "1",
                        Prefix = "--revision=",
                        Order = 2
                    },
                    new ChatToolParameterDefinition
                    {
                        Name = "change",
                        Label = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.EditWorkflow.Parameter.Change",
                            "Requested change"),
                        Hint = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.EditWorkflow.Parameter.ChangeHint",
                            "Describe the smallest workflow change to apply"),
                        Placeholder = NeuCharWorkflowResource.Get(
                            "AdminChat.Command.EditWorkflow.Parameter.ChangePlaceholder",
                            "Describe the requested change"),
                        IsPositional = true,
                        Required = true,
                        Order = 3
                    }
                },
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.SearchWorkflows)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.InspectWorkflow)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(NeuCharWorkflowChatToolAppService),
                        MethodName = nameof(NeuCharWorkflowChatToolAppService.ApplyWorkflowEdit)
                    }
                },
                Instructions = """

[Workflow Editing Assistant]
Help the user edit an existing NeuChar Workflow. Only this command's explicit tools are available.
1. Resolve the workflow by explicit --id and optional --revision, or SearchWorkflows, then call InspectWorkflow.
2. Review the masked graph and the current revision. Never reconstruct protected parameter values.
3. For a clear, concrete request, produce the smallest valid graph change and call ApplyWorkflowEdit with the inspected revision exactly once.
4. If the requested change is ambiguous, or if a revision conflict occurs, stop and ask; do not create another workflow.
5. Preserve masked secret placeholders and existing workflow identity, trigger, enabled state, variables and unrelated nodes.
6. Never call StartWorkflow from this command. After saving, report the new revision and tell the user to reload the editor.
7. Never ask for or synthesize credentials or external-resume secrets.
"""
            }
        };
    }

    private static void AddChatToolServices(IServiceCollection services)
    {
        services.AddScoped<WorkflowChatToolParameterOptionsProvider>();
    }
}

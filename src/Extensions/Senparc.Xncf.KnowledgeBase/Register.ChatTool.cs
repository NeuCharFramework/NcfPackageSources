/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.ChatTool.cs
    File description: AdminChat command registration and chat tool services

    Created by: Senparc - 20261125

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.ChatTools;
using Senparc.Xncf.KnowledgeBase.Application.ChatTools;
using Senparc.Xncf.KnowledgeBase.OHS.Local.AppService;
using System.Collections.Generic;

namespace Senparc.Xncf.KnowledgeBase;

public partial class Register : IChatTool
{
    /// <summary>
    /// Returns the AdminChat command definition for indexed knowledge-base search and recall.
    /// </summary>
    /// <returns>The registered chat tool definitions.</returns>
    public IReadOnlyCollection<ChatToolDefinition> GetChatTools()
    {
        return new[]
        {
            new ChatToolDefinition
            {
                Command = "/search-knowledge-base",
                Label = KnowledgeBaseResource.Get(
                    "AdminChat.Command.SearchKnowledgeBase",
                    "Search a knowledge base"),
                Hint = KnowledgeBaseResource.Get(
                    "AdminChat.Command.SearchKnowledgeBaseHint",
                    "Choose a knowledge base and enter a question"),
                ModuleName = MenuName,
                ModuleUid = Uid,
                Parameters = new[]
                {
                    new ChatToolParameterDefinition
                    {
                        Name = "id",
                        Label = KnowledgeBaseResource.Get(
                            "AdminChat.Command.SearchKnowledgeBase.Parameter.Id",
                            "Knowledge base"),
                        Hint = KnowledgeBaseResource.Get(
                            "AdminChat.Command.SearchKnowledgeBase.Parameter.IdHint",
                            "Choose an indexed knowledge base"),
                        Placeholder = "knowledgeBaseId",
                        Prefix = "--id=",
                        Required = true,
                        OptionsProviderType = typeof(KnowledgeBaseChatToolParameterOptionsProvider)
                    },
                    new ChatToolParameterDefinition
                    {
                        Name = "question",
                        Label = KnowledgeBaseResource.Get(
                            "AdminChat.Command.SearchKnowledgeBase.Parameter.Question",
                            "Question"),
                        Hint = KnowledgeBaseResource.Get(
                            "AdminChat.Command.SearchKnowledgeBase.Parameter.QuestionHint",
                            "Ask a question to retrieve and answer from the selected knowledge base"),
                        Placeholder = KnowledgeBaseResource.Get(
                            "AdminChat.Command.SearchKnowledgeBase.Parameter.QuestionPlaceholder",
                            "Enter a question"),
                        IsPositional = true,
                        Required = true,
                        Order = 2
                    }
                },
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(KnowledgeBaseChatToolAppService),
                        MethodName = nameof(KnowledgeBaseChatToolAppService.SearchKnowledgeBases)
                    },
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(KnowledgeBaseChatToolAppService),
                        MethodName = nameof(KnowledgeBaseChatToolAppService.RecallKnowledgeBase)
                    }
                },
                Instructions = """

[Knowledge Base Search Command]
Answer the user's question only with evidence retrieved from an existing indexed knowledge base.
1. Resolve --id explicitly, or use SearchKnowledgeBases when the user describes a knowledge base.
2. If several knowledge bases plausibly match, show the options and ask the user to choose.
3. Call RecallKnowledgeBase exactly once with the selected ID and the user's question.
4. Cite the returned source name or source link for every substantive answer.
5. If no useful passages are returned, say so and do not invent an answer.
6. Do not create, update, embed, or delete knowledge bases in this command.
"""
            }
        };
    }

    /// <summary>
    /// Registers services used by the knowledge-base chat-tool parameter providers.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    private static void AddChatToolServices(IServiceCollection services)
    {
        services.AddScoped<KnowledgeBaseChatToolParameterOptionsProvider>();
    }
}

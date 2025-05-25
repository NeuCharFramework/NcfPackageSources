// Copyright (c) Microsoft Corporation. All rights reserved.
// RolePlayOrchestrator.cs

using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoGen.Core;

public class MyRolePlayOrchestratorReply
{
    public string From { get; set; }
    public string Message { get; set; }
}

public class MyRolePlayOrchestrator : IOrchestrator
{
    private readonly IAgent admin;
    private readonly Graph? workflow;
    public MyRolePlayOrchestrator(IAgent admin, Graph? workflow = null)
    {
        this.admin = admin;
        this.workflow = workflow;
    }

    public async Task<IAgent?> GetNextSpeakerAsync(
        OrchestrationContext context,
        CancellationToken cancellationToken = default)
    {
        var candidates = context.Candidates.ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates.First();
        }

        // if there's a workflow
        // and the next available agent from the workflow is in the group chat
        // then return the next agent from the workflow
        if (this.workflow != null)
        {
            var lastMessage = context.ChatHistory.LastOrDefault();
            if (lastMessage == null)
            {
                return null;
            }
            var currentSpeaker = candidates.First(candidates => candidates.Name == lastMessage.From);
            var nextAgents = await this.workflow.TransitToNextAvailableAgentsAsync(currentSpeaker, context.ChatHistory, cancellationToken);
            nextAgents = nextAgents.Where(nextAgent => candidates.Any(candidate => candidate.Name == nextAgent.Name));
            candidates = nextAgents.ToList();
            if (!candidates.Any())
            {
                return null;
            }

            if (candidates is { Count: 1 })
            {
                return candidates.First();
            }
        }

        // In this case, since there are more than one available agents from the workflow for the next speaker
        // the admin will be invoked to decide the next speaker
        var agentNames = candidates.Select(candidate => candidate.Name);
        var rolePlayMessage = new TextMessage(Role.User,
            content: $@"You are in a role play game. Carefully read the conversation history and carry on the conversation.
The available roles are:
{agentNames.ToJson()}

Each message will use the strickly JSON format with a '//finish suffix':
{{From:""<From Agent Name>"", Message:""<Chat Message>""}}//finish

e,g:
{{From:""{agentNames.First()}"", Message:""Hi, I'm {agentNames.First()}.""}}//finish

Note: parameter From must be strictly equal to the name of the player spokesperson and cannot be modified in any way.
");

        var chatHistoryWithName = this.ProcessConversationsForRolePlay(context.ChatHistory);
        var messages = new IMessage[] { rolePlayMessage }.Concat(chatHistoryWithName);

        var response = await this.admin.GenerateReplyAsync(
            messages: messages,
            options: new GenerateReplyOptions
            {
                Temperature = 0,
                MaxToken = 256,
                StopSequence = [":", "//finish"],
                Functions = null,
            },
            cancellationToken: cancellationToken);

        var responseMessageStr = response.GetContent() ?? throw new ArgumentException("No name is returned.");

        MyRolePlayOrchestratorReply responseMessage = null;
        try
        {
            responseMessage = responseMessageStr.GetObject<MyRolePlayOrchestratorReply>();

        }
        catch (Exception ex)
        {
            throw;
        }

        var name = responseMessage.From;
        var candidate = candidates.FirstOrDefault(x => x.Name!.ToLower() == name.ToLower());

        if (candidate != null)
        {
            return candidate;
        }

        var errorMessage = $"The response from admin is {name}, which is either not in the candidates list or not in the correct format.";
        throw new ArgumentException(errorMessage);
    }

    private IEnumerable<IMessage> ProcessConversationsForRolePlay(IEnumerable<IMessage> messages)
    {
        return messages.Select((x, i) =>
        {
            //            var msg = @$"From {x.From}:
            //{x.GetContent()}
            //<eof_msg>
            //round # {i}";

            var msg = new
            {
                From = x.From,
                Message = x.GetContent(),
                Round = $"#{i}"
            }.ToJson();

            return new TextMessage(Role.User, content: msg);
        });
    }
}

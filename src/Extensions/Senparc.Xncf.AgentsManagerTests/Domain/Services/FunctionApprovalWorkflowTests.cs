using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins;
using System.Runtime.CompilerServices;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class FunctionApprovalWorkflowTests
{
    [TestMethod]
    public void CrawlOptionalParametersUseSafeDefaults()
    {
        var method = typeof(CrawlPlugin).GetMethod(nameof(CrawlPlugin.Crawl));
        Assert.IsNotNull(method);
        var parameters = method.GetParameters();

        Assert.AreEqual("url", parameters[0].Name);
        Assert.AreEqual("maxDepth", parameters[1].Name);
        Assert.IsTrue(parameters[1].HasDefaultValue);
        Assert.AreEqual(1, parameters[1].DefaultValue);
        Assert.IsTrue(parameters[2].HasDefaultValue);
        Assert.AreEqual(5, parameters[2].DefaultValue);
        Assert.IsTrue(parameters[3].HasDefaultValue);
        Assert.AreEqual(string.Empty, parameters[3].DefaultValue);
    }

    [TestMethod]
    public async Task ApprovedFunctionCallInWorkflowInvokesToolAndCompletes()
    {
        var tool = new RecordingTool();
        var innerClient = new FunctionRequestChatClient();
        using var invokingClient = new FunctionInvokingChatClient(innerClient);
        var function = AIFunctionFactory.Create(
            (Func<string, string>)tool.Crawl,
            name: "Crawl");
        var agent = new ChatClientAgent(
            invokingClient,
            new ChatClientAgentOptions
            {
                Name = "Crawler",
                ChatOptions = new ChatOptions
                {
                    Tools = [new ApprovalRequiredAIFunction(function)],
                    AllowMultipleToolCalls = false
                }
            });
        var workflow = AgentWorkflowBuilder.BuildSequential([agent]);

        await using var run = await InProcessExecution.RunStreamingAsync(
            workflow,
            new List<ChatMessage> { new(ChatRole.User, "读取网页") });
        Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));

        var firstEvents = await ReadUntilHaltAsync(run);
        var requestEvent = firstEvents.OfType<RequestInfoEvent>().Single();
        var approvalRequest = requestEvent.Request.Data.As<ToolApprovalRequestContent>();
        Assert.IsNotNull(approvalRequest);
        Assert.AreEqual(RunStatus.PendingRequests, await run.GetStatusAsync());

        await run.SendResponseAsync(requestEvent.Request.CreateResponse(
            approvalRequest.CreateResponse(true, "测试批准")));
        Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));

        var secondEvents = await ReadUntilHaltAsync(run);
        Assert.AreEqual(1, tool.InvocationCount);
        Assert.AreEqual("https://www.ncf.pub", tool.LastUrl);
        Assert.IsTrue(secondEvents
            .OfType<AgentResponseUpdateEvent>()
            .Any(z => z.Update.Text.Contains("工具完成", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task ApprovedFunctionCallInGroupChatInvokesTool()
    {
        var tool = new RecordingTool();
        using var toolClient = new FunctionInvokingChatClient(new FunctionRequestChatClient());
        var function = AIFunctionFactory.Create(
            (Func<string, string>)tool.Crawl,
            name: "Crawl");
        var toolAgent = new ChatClientAgent(
            toolClient,
            new ChatClientAgentOptions
            {
                Name = "Crawler",
                ChatOptions = new ChatOptions
                {
                    Tools = [new ApprovalRequiredAIFunction(function)],
                    AllowMultipleToolCalls = false
                }
            });
        var passiveAgent = new ChatClientAgent(
            new TextChatClient("等待爬虫结果"),
            new ChatClientAgentOptions { Name = "Coordinator" });
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
            {
                var manager = new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 4
                };
                return manager;
            })
            .AddParticipants([toolAgent, passiveAgent])
            .Build();

        await using var run = await InProcessExecution.RunStreamingAsync(
            workflow,
            new List<ChatMessage> { new(ChatRole.User, "读取网页") });

        RequestInfoEvent? requestEvent = null;
        for (var turn = 0; turn < 4 && requestEvent == null; turn++)
        {
            Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));
            requestEvent = (await ReadUntilHaltAsync(run))
                .OfType<RequestInfoEvent>()
                .FirstOrDefault();
        }

        Assert.IsNotNull(requestEvent);
        var approvalRequest = requestEvent.Request.Data.As<ToolApprovalRequestContent>();
        Assert.IsNotNull(approvalRequest);

        await run.SendResponseAsync(requestEvent.Request.CreateResponse(
            approvalRequest.CreateResponse(true, "测试批准")));
        Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));
        await ReadUntilHaltAsync(run);

        Assert.AreEqual(1, tool.InvocationCount);
        Assert.AreEqual("https://www.ncf.pub", tool.LastUrl);
    }

    [TestMethod]
    public async Task ApprovedFunctionCallInGroupChatResumesWithoutAdditionalTurnToken()
    {
        var tool = new RecordingTool();
        using var toolClient = new FunctionInvokingChatClient(new FunctionRequestChatClient());
        var function = AIFunctionFactory.Create(
            (Func<string, string>)tool.Crawl,
            name: "Crawl");
        var toolAgent = new ChatClientAgent(
            toolClient,
            new ChatClientAgentOptions
            {
                Name = "Crawler",
                ChatOptions = new ChatOptions
                {
                    Tools = [new ApprovalRequiredAIFunction(function)],
                    AllowMultipleToolCalls = false
                }
            });
        var passiveAgent = new ChatClientAgent(
            new TextChatClient("等待爬虫结果"),
            new ChatClientAgentOptions { Name = "Coordinator" });
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
            {
                var manager = new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 4
                };
                return manager;
            })
            .AddParticipants([toolAgent, passiveAgent])
            .Build();

        await using var run = await InProcessExecution.RunStreamingAsync(
            workflow,
            new List<ChatMessage> { new(ChatRole.User, "读取网页") });

        RequestInfoEvent? requestEvent = null;
        for (var turn = 0; turn < 4 && requestEvent == null; turn++)
        {
            Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));
            requestEvent = (await ReadUntilHaltAsync(run))
                .OfType<RequestInfoEvent>()
                .FirstOrDefault();
        }

        Assert.IsNotNull(requestEvent);
        var approvalRequest = requestEvent.Request.Data.As<ToolApprovalRequestContent>();
        Assert.IsNotNull(approvalRequest);

        await run.SendResponseAsync(requestEvent.Request.CreateResponse(
            approvalRequest.CreateResponse(true, "测试批准")));
        await ReadUntilHaltAsync(run);

        Assert.AreEqual(1, tool.InvocationCount);
        Assert.AreEqual("https://www.ncf.pub", tool.LastUrl);
    }

    [TestMethod]
    public async Task RequestStoreResolutionResumesApprovedFunctionCallInGroupChat()
    {
        var tool = new RecordingTool();
        using var toolClient = new FunctionInvokingChatClient(new FunctionRequestChatClient());
        var function = AIFunctionFactory.Create(
            (Func<string, string>)tool.Crawl,
            name: "Crawl");
        var toolAgent = new ChatClientAgent(
            toolClient,
            new ChatClientAgentOptions
            {
                Name = "Crawler",
                ChatOptions = new ChatOptions
                {
                    Tools = [new ApprovalRequiredAIFunction(function)],
                    AllowMultipleToolCalls = false
                }
            });
        var passiveAgent = new ChatClientAgent(
            new TextChatClient("等待爬虫结果"),
            new ChatClientAgentOptions { Name = "Coordinator" });
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
            {
                var manager = new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 4
                };
                return manager;
            })
            .AddParticipants([toolAgent, passiveAgent])
            .Build();

        await using var run = await InProcessExecution.RunStreamingAsync(
            workflow,
            new List<ChatMessage> { new(ChatRole.User, "读取网页") });

        RequestInfoEvent? requestEvent = null;
        for (var turn = 0; turn < 4 && requestEvent == null; turn++)
        {
            Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));
            requestEvent = (await ReadUntilHaltAsync(run))
                .OfType<RequestInfoEvent>()
                .FirstOrDefault();
        }

        Assert.IsNotNull(requestEvent);
        var approvalRequest = requestEvent.Request.Data.As<ToolApprovalRequestContent>();
        Assert.IsNotNull(approvalRequest);

        var store = new HumanInTheLoopRequestStore();
        var pending = store.RegisterWorkflowToolApproval(
            42,
            "Crawler",
            requestEvent.Request,
            approvalRequest);
        Assert.IsTrue(store.TryResolve(
            pending.RequestId,
            new HumanInTheLoopDecision(true, "测试批准"),
            out _));
        Assert.IsInstanceOfType<ExternalResponse>(pending.ResolvedResponse);

        await run.SendResponseAsync((ExternalResponse)pending.ResolvedResponse);
        Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));
        await ReadUntilHaltAsync(run);

        Assert.AreEqual(1, tool.InvocationCount);
        Assert.AreEqual("https://www.ncf.pub", tool.LastUrl);
    }

    private static async Task<List<WorkflowEvent>> ReadUntilHaltAsync(
        StreamingRun run,
        CancellationToken cancellationToken = default)
    {
        var events = new List<WorkflowEvent>();
        await foreach (var workflowEvent in run.WatchStreamAsync(cancellationToken))
        {
            events.Add(workflowEvent);
        }

        return events;
    }

    private sealed class RecordingTool
    {
        public int InvocationCount { get; private set; }
        public string LastUrl { get; private set; } = string.Empty;

        public string Crawl(string url)
        {
            InvocationCount++;
            LastUrl = url;
            return "网页内容";
        }
    }

    private sealed class FunctionRequestChatClient : IChatClient
    {
        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient)
                ? this
                : serviceType == typeof(ChatClientMetadata)
                    ? new ChatClientMetadata("test")
                    : null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var response = BuildResponse(messages);
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in BuildResponse(messages).ToChatResponseUpdates())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
                await Task.Yield();
            }
        }

        private static ChatResponse BuildResponse(IEnumerable<ChatMessage> messages)
        {
            var history = messages.ToList();
            if (history
                .SelectMany(z => z.Contents)
                .OfType<FunctionResultContent>()
                .Any())
            {
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "工具完成"));
            }

            return new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                new List<AIContent>
                {
                    new FunctionCallContent(
                        "call-crawl-1",
                        "Crawl",
                        new Dictionary<string, object?>
                        {
                            ["url"] = "https://www.ncf.pub"
                        })
                }));
        }
    }

    private sealed class TextChatClient(string text) : IChatClient
    {
        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IChatClient)
                ? this
                : serviceType == typeof(ChatClientMetadata)
                    ? new ChatClientMetadata("test")
                    : null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            await Task.Yield();
        }
    }
}

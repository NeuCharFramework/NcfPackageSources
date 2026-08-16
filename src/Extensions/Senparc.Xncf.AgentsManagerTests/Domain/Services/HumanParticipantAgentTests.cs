using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Senparc.Xncf.AgentsManager.Domain.Services;
using System.Runtime.CompilerServices;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class HumanParticipantAgentTests
{
    [TestMethod]
    public async Task HumanAgentWaitsForAndReturnsSubmittedText()
    {
        var store = new HumanInTheLoopRequestStore();
        var requestCreated = new TaskCompletionSource<PendingHumanRequest>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = new HumanParticipantAgent(
            store,
            42,
            "human:7",
            pending =>
            {
                requestCreated.TrySetResult(pending);
                return Task.CompletedTask;
            },
            (_, _) => Task.CompletedTask);

        var runTask = agent.RunAsync("上一位 Agent 的消息");
        var pending = await requestCreated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(42, pending.ChatTaskId);
        Assert.AreEqual("human:7", pending.ParticipantKey);
        Assert.AreEqual("humanTurn", pending.RequestType);
        Assert.IsTrue(pending.Prompt.Contains("上一位 Agent 的消息", StringComparison.Ordinal));

        Assert.IsTrue(store.TryResolve(
            pending.RequestId,
            new HumanInTheLoopDecision(true, "测试回复", "用户实际输入"),
            out _));

        var response = await runTask;
        Assert.AreEqual("用户实际输入", response.Text);
        Assert.AreEqual(0, store.GetPending(42).Count);
    }

    [TestMethod]
    public async Task GroupChatSchedulesHumanAfterAnAgentAsksForMoreInformation()
    {
        var store = new HumanInTheLoopRequestStore();
        var requestCreated = new TaskCompletionSource<PendingHumanRequest>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var human = new HumanParticipantAgent(
            store,
            42,
            "human:7",
            pending =>
            {
                requestCreated.TrySetResult(pending);
                return Task.CompletedTask;
            },
            (_, _) => Task.CompletedTask);
        var translator = new ChatClientAgent(
            new TextChatClient("请补充以“请你”开头的翻译请求，并注明目标语言。"),
            new ChatClientAgentOptions { Name = "翻译 Agent" });
        var workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
            {
                var manager = new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 4
                };
                return manager;
            })
            .AddParticipants([translator, human])
            .Build();

        await using var run = await InProcessExecution.RunStreamingAsync(
            workflow,
            new List<ChatMessage> { new(ChatRole.User, "翻译：苏州是美丽的城市") });

        for (var turn = 0; turn < 4; turn++)
        {
            Assert.IsTrue(await run.TrySendMessageAsync(new TurnToken(emitEvents: true)));
            var watchTask = ReadUntilHaltAsync(run);
            var completed = await Task.WhenAny(requestCreated.Task, watchTask);
            if (completed != requestCreated.Task)
            {
                await watchTask;
                continue;
            }

            var pending = await requestCreated.Task;
            Assert.IsTrue(pending.Prompt.Contains("请补充以“请你”开头", StringComparison.Ordinal));
            Assert.IsTrue(store.TryResolve(
                pending.RequestId,
                new HumanInTheLoopDecision(true, "补充请求", "请你把苏州翻译成英文"),
                out _));

            var humanEvents = await watchTask;
            Assert.IsTrue(humanEvents
                .OfType<AgentResponseUpdateEvent>()
                .Any(z => z.Update.Text.Contains("请你把苏州翻译成英文", StringComparison.Ordinal)));
            return;
        }

        Assert.Fail("GroupChat did not schedule the configured Human participant.");
    }

    private static async Task<List<WorkflowEvent>> ReadUntilHaltAsync(StreamingRun run)
    {
        var events = new List<WorkflowEvent>();
        await foreach (var workflowEvent in run.WatchStreamAsync())
        {
            events.Add(workflowEvent);
        }

        return events;
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

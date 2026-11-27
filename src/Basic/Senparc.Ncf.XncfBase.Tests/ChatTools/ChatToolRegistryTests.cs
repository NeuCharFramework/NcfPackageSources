using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.XncfBase.ChatTools;

namespace Senparc.Ncf.XncfBase.Tests.ChatTools;

[TestClass]
public class ChatToolRegistryTests
{
    [TestMethod]
    public void MatchRequiresACompleteCommandToken()
    {
        var tools = CreateProvider().GetChatTools();

        var exact = ChatToolRegistry.Match("/analyze-system-logs today", tools);
        var prefix = ChatToolRegistry.Match("/analyze-system-logs-all", tools);

        Assert.AreEqual(1, exact.Count);
        Assert.AreEqual(0, prefix.Count);
    }

    [TestMethod]
    public void GetChatToolsDeduplicatesCommandsAndIgnoresInvalidProviders()
    {
        var provider = CreateProvider();
        var invalid = new TestChatToolProvider(Array.Empty<ChatToolDefinition>());

        var tools = ChatToolRegistry.GetChatTools(new IChatTool[] { provider, provider, invalid });

        Assert.AreEqual(1, tools.Count);
        Assert.AreEqual("/analyze-system-logs", tools.First().Command);
    }

    [TestMethod]
    public async Task ParameterOptionsSupportsStaticFiltering()
    {
        var tool = CreateParameterizedTool();

        var result = await ChatToolRegistry.GetParameterOptionsAsync(
            new EmptyServiceProvider(),
            new ChatToolParameterOptionsRequest
            {
                Command = tool.Command,
                Parameter = "group",
                Query = "support"
            },
            new[] { tool });

        Assert.AreEqual(1, result.Options.Count);
        Assert.AreEqual("7", result.Options.Single().Value);
    }

    [TestMethod]
    public async Task ParameterOptionsUsesRegisteredModuleProvider()
    {
        var tool = CreateParameterizedTool(typeof(TestParameterOptionsProvider));

        var result = await ChatToolRegistry.GetParameterOptionsAsync(
            new TestServiceProvider(new TestParameterOptionsProvider()),
            new ChatToolParameterOptionsRequest
            {
                Command = tool.Command,
                Parameter = "group",
                Query = "chat"
            },
            new[] { tool });

        Assert.AreEqual("42", result.Options.Single().Value);
    }

    [TestMethod]
    public void ExtractAsyncActionsReadsNestedFunctionResponseAndDeduplicates()
    {
        var result = new
        {
            Success = true,
            Data = JsonSerializer.Serialize(new
            {
                kind = "admin-chat-action",
                actions = new[]
                {
                    new
                    {
                        type = "agents-task",
                        task = new { ChatTaskId = 6012, ChatGroupName = "Function group" }
                    }
                }
            })
        };

        var actions = ChatToolRegistry.ExtractAsyncActions(result);

        Assert.AreEqual(1, actions.Count);
        Assert.AreEqual("agents-task", actions[0].ActionType);
        Assert.AreEqual("6012", actions[0].Target);
        Assert.AreEqual("Function group", actions[0].Name);
    }

    private static TestChatToolProvider CreateProvider()
    {
        return new TestChatToolProvider(new[]
        {
            new ChatToolDefinition
            {
                Command = "/analyze-system-logs",
                Label = "Analyze logs",
                ModuleName = "Admin",
                ModuleUid = "admin",
                Functions = new[]
                {
                    new ChatToolFunctionDefinition
                    {
                        DeclaringType = typeof(ChatToolRegistryTests),
                        MethodName = nameof(MatchRequiresACompleteCommandToken)
                    }
                }
            }
        });
    }

    private static ChatToolDefinition CreateParameterizedTool(Type optionsProviderType = null)
    {
        return new ChatToolDefinition
        {
            Command = "/run-group",
            Label = "Run group",
            ModuleName = "Agents",
            Functions = new[]
            {
                new ChatToolFunctionDefinition
                {
                    DeclaringType = typeof(ChatToolRegistryTests),
                    MethodName = nameof(MatchRequiresACompleteCommandToken)
                }
            },
            Parameters = new[]
            {
                new ChatToolParameterDefinition
                {
                    Name = "group",
                    Label = "Group",
                    Prefix = "--group=",
                    OptionsProviderType = optionsProviderType,
                    Options = new[]
                    {
                        new ChatToolParameterOption
                        {
                            Value = "3",
                            Label = "Research",
                            Description = "Knowledge discovery"
                        },
                        new ChatToolParameterOption
                        {
                            Value = "7",
                            Label = "Customer support",
                            Description = "Answer requests"
                        }
                    }
                }
            }
        };
    }

    private sealed class TestChatToolProvider(IReadOnlyCollection<ChatToolDefinition> tools) : IChatTool
    {
        public IReadOnlyCollection<ChatToolDefinition> GetChatTools() => tools;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => null;
    }

    private sealed class TestServiceProvider(object service) : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return serviceType.IsInstanceOfType(service) ? service : null;
        }
    }

    private sealed class TestParameterOptionsProvider : IChatToolParameterOptionsProvider
    {
        public Task<IReadOnlyCollection<ChatToolParameterOption>> GetOptionsAsync(
            ChatToolParameterOptionsRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ChatToolParameterOption>>(new[]
            {
                new ChatToolParameterOption
                {
                    Value = "42",
                    Label = request.Query
                }
            });
        }
    }
}

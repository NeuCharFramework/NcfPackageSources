using System;
using System.Collections.Generic;
using System.Linq;
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

    private sealed class TestChatToolProvider(IReadOnlyCollection<ChatToolDefinition> tools) : IChatTool
    {
        public IReadOnlyCollection<ChatToolDefinition> GetChatTools() => tools;
    }
}

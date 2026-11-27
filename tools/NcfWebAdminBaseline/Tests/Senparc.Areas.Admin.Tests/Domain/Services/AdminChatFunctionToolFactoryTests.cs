using Microsoft.Extensions.AI;
using Moq;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatFunctionToolFactoryTests
{
    [TestMethod]
    public async Task WorkflowManagement_CreateToolRequiresApprovalAndValidationToolIsReadOnly()
    {
        var provider = new Mock<IWorkflowCreationProvider>();
        provider.Setup(value => value.GetCapabilitiesAsync(
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowCreationCapabilities(
                new[] { "manual" },
                new[] { "manual-trigger", "console", "neubell", "end" },
                Array.Empty<WorkflowCreationFunctionDescriptor>(),
                30,
                60));
        provider.Setup(value => value.ValidateAsync(
                7,
                It.IsAny<WorkflowCreationRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowCreationValidationResult(
                true,
                "{\"nodes\":[],\"edges\":[]}",
                4,
                3,
                Array.Empty<string>()));

        var tools = AdminChatWorkflowManagementToolFactory.Create(provider.Object, 7);
        var createTool = tools.Single(value => value.Name == "workflow_create");
        var validateTool = tools.Single(value => value.Name == "workflow_validate_creation");

        Assert.IsInstanceOfType<ApprovalRequiredAIFunction>(createTool);

        var validationResult = await validateTool.InvokeAsync(new AIFunctionArguments
        {
            ["name"] = "FromChat",
            ["triggerType"] = "manual",
            ["graphJson"] = "{\"nodes\":[],\"edges\":[]}",
            ["enabled"] = true
        });

        StringAssert.Contains(validationResult?.ToString(), "\"success\":true");
        provider.Verify(value => value.ValidateAsync(
            7,
            It.Is<WorkflowCreationRequest>(request =>
                request.Name == "FromChat"
                && request.TriggerType == "manual"
                && request.Enabled),
            It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(value => value.CreateAsync(
            It.IsAny<int>(),
            It.IsAny<WorkflowCreationRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_BindsInstanceFunctionToItsPlugin()
    {
        var plugin = new RecordingPlugin();
        var method = typeof(RecordingPlugin).GetMethod(nameof(RecordingPlugin.Echo));

        var function = AdminChatFunctionToolFactory.Create(
            method,
            plugin,
            "Xncf_RecordingPlugin_Echo",
            "Returns the supplied value.");

        await function.InvokeAsync(new AIFunctionArguments
        {
            ["value"] = "AdminChat"
        });

        Assert.AreEqual("Xncf_RecordingPlugin_Echo", function.Name);
        Assert.AreEqual(1, plugin.InvocationCount);
        Assert.AreEqual("AdminChat", plugin.LastValue);
    }

    [TestMethod]
    public async Task WorkspaceWrite_RejectsEmptyRelativePath()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var write = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_write");

        var result = await write.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = string.Empty,
            ["content"] = "<html></html>"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        Assert.IsFalse(document.RootElement.GetProperty("success").GetBoolean());
        StringAssert.Contains(document.RootElement.GetProperty("error").GetString(), "relativePath 不能为空");
        client.Verify(
            value => value.WriteFileAsync(
                It.IsAny<WorkspaceWriteFileRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task WorkspaceWrite_ReturnsRequiredArgumentsWhenInvocationOmitsThem()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var write = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_write");

        var result = await write.InvokeAsync(new AIFunctionArguments());

        using var document = JsonDocument.Parse(result!.ToString()!);
        Assert.IsFalse(document.RootElement.GetProperty("success").GetBoolean());
        CollectionAssert.AreEqual(
            new[] { "relativePath", "content" },
            document.RootElement.GetProperty("requiredArguments")
                .EnumerateArray()
                .Select(value => value.GetString())
                .ToArray());
    }

    [TestMethod]
    public async Task WorkspaceList_DefaultsMissingOrNullRelativePathToRoot()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        client.Setup(value => value.ListAsync(
                string.Empty,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WorkspaceEntry>());
        var list = CreateWorkspaceTools(client.Object, allowWrite: false)
            .Single(tool => tool.Name == "workspace_list");

        await list.InvokeAsync(new AIFunctionArguments());
        await list.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = null
        });

        client.Verify(value => value.ListAsync(
            string.Empty,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task WorkspaceRead_ClampsRequestedCharactersToSafeMaximum()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var metadata = new WorkspaceFileMetadata(
            "large.txt",
            false,
            6000,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"etag\"");
        client.Setup(value => value.ReadFileAsync(
                "large.txt",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                metadata,
                Encoding.UTF8.GetBytes(new string('a', 6000))));
        var read = CreateWorkspaceTools(client.Object, allowWrite: false)
            .Single(tool => tool.Name == "workspace_read");

        var result = await read.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "large.txt",
            ["maxCharacters"] = 6000
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        Assert.AreEqual(4000, document.RootElement.GetProperty("content").GetString()!.Length);
        Assert.AreEqual(4000, document.RootElement.GetProperty("nextOffset").GetInt32());
        Assert.IsTrue(document.RootElement.GetProperty("hasMore").GetBoolean());
    }

    [TestMethod]
    public void WorkspaceTools_HidePythonExecutionWhenRuntimeCannotExecute()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");

        var tools = CreateWorkspaceTools(client.Object, allowWrite: true, allowCli: false);

        Assert.IsFalse(tools.Any(tool => tool.Name == "workspace_execute_python"));
    }

    [TestMethod]
    public void WorkspaceSystemPrompt_ExplainsRootPathsAndOutputFolders()
    {
        var method = typeof(AdminChatAiService).GetMethod(
            "BuildSystemMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var prompt = method!.Invoke(null, [null])!.ToString()!;

        StringAssert.Contains(prompt, "relativePath may be omitted or empty");
        StringAssert.Contains(prompt, "outputs/<short-task-name>/");
        StringAssert.Contains(prompt, "code execution is disabled");
    }

    [TestMethod]
    public void AdminChatSamplingParameters_AreDisabledForReasoningModels()
    {
        var method = typeof(AdminChatAiService).GetMethod(
            "SupportsSamplingParameters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.IsFalse((bool)method!.Invoke(null, ["gpt-5.6-luna"])!);
        Assert.IsFalse((bool)method.Invoke(null, ["openai/o3"])!);
        Assert.IsTrue((bool)method.Invoke(null, ["gpt-4.1-mini"])!);
    }

    [TestMethod]
    public async Task WorkspaceWrite_ReturnsStructuredLineChangeSummary()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var metadata = new WorkspaceFileMetadata(
            "index.html",
            false,
            12,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"etag\"");
        client.SetupSequence(value => value.GetFileInfoAsync(
                "index.html",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException())
            .ReturnsAsync(metadata);
        client.Setup(value => value.ReadFileAsync(
                "index.html",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                metadata,
                Encoding.UTF8.GetBytes("<p>one</p>\n<p>two</p>\n")));
        client.Setup(value => value.WriteFileAsync(
                It.IsAny<WorkspaceWriteFileRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceWriteFileResult(metadata, true));

        var write = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_write");
        var result = await write.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "index.html",
            ["content"] = "<p>one</p>\n<p>two</p>\n"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        var change = document.RootElement.GetProperty("change");
        Assert.AreEqual("index.html", change.GetProperty("path").GetString());
        Assert.AreEqual("added", change.GetProperty("changeType").GetString());
        Assert.AreEqual(2, change.GetProperty("addedLines").GetInt32());
        Assert.AreEqual(0, change.GetProperty("removedLines").GetInt32());
    }

    [TestMethod]
    public async Task WorkspacePatch_ReturnsAddedAndRemovedLineCounts()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var beforeMetadata = new WorkspaceFileMetadata(
            "index.html",
            false,
            16,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"before\"");
        var afterMetadata = beforeMetadata with
        {
            Length = 25,
            ETag = "\"after\""
        };
        client.SetupSequence(value => value.GetFileInfoAsync(
                "index.html",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(beforeMetadata)
            .ReturnsAsync(afterMetadata);
        client.SetupSequence(value => value.ReadFileAsync(
                "index.html",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                beforeMetadata,
                Encoding.UTF8.GetBytes("one\ntwo\n")))
            .ReturnsAsync(new WorkspaceFileContent(
                afterMetadata,
                Encoding.UTF8.GetBytes("one\nthree\nfour\n")));
        client.Setup(value => value.ApplyPatchAsync(
                It.IsAny<WorkspacePatchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspacePatchResult(true, 1, "补丁应用成功。"));

        var patch = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_patch");
        var result = await patch.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "index.html",
            ["oldText"] = "two",
            ["newText"] = "three\nfour"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        var change = document.RootElement.GetProperty("change");
        Assert.AreEqual(2, change.GetProperty("addedLines").GetInt32());
        Assert.AreEqual(1, change.GetProperty("removedLines").GetInt32());
    }

    [TestMethod]
    public async Task WorkspaceDelete_ReturnsRemovedLineCount()
    {
        var client = new Mock<IWorkspaceClient>();
        client.SetupGet(value => value.WorkspaceId).Returns("ws-test");
        var metadata = new WorkspaceFileMetadata(
            "obsolete.txt",
            false,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "\"etag\"");
        client.Setup(value => value.GetFileInfoAsync(
                "obsolete.txt",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        client.Setup(value => value.ReadFileAsync(
                "obsolete.txt",
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceFileContent(
                metadata,
                Encoding.UTF8.GetBytes("first\nsecond\n")));

        var delete = CreateWorkspaceTools(client.Object, allowWrite: true)
            .Single(tool => tool.Name == "workspace_delete");
        client.Setup(value => value.DeleteEntryAsync(
                It.IsAny<WorkspaceDeleteEntryRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var result = await delete.InvokeAsync(new AIFunctionArguments
        {
            ["relativePath"] = "obsolete.txt"
        });

        using var document = JsonDocument.Parse(result!.ToString()!);
        var change = document.RootElement.GetProperty("change");
        Assert.AreEqual("deleted", change.GetProperty("changeType").GetString());
        Assert.AreEqual(0, change.GetProperty("addedLines").GetInt32());
        Assert.AreEqual(2, change.GetProperty("removedLines").GetInt32());
    }

    [TestMethod]
    public void Create_ExposesSelectionValuesAndDisplayTextInSchema()
    {
        var plugin = new RecordingPlugin();
        var method = typeof(RecordingPlugin).GetMethod(nameof(RecordingPlugin.Select));
        var function = AdminChatFunctionToolFactory.Create(
            method,
            plugin,
            "Xncf_RecordingPlugin_Select",
            "Selects a sandbox template.",
            new[]
            {
                new FunctionParameterInfo
                {
                    Name = nameof(SelectionRequest.TemplateKey),
                    ParameterType = ParameterType.DropDownList,
                    SelectionList = new SelectionList(
                        SelectionType.DropDownList,
                        new[]
                        {
                            new SelectionItem("python-exec", "Python Exec", "short Python workload"),
                            new SelectionItem("csharp-exec", "C# Exec", "short C# workload")
                        })
                }
            });

        using var schema = JsonDocument.Parse(function.JsonSchema.GetRawText());
            Assert.IsTrue(schema.RootElement.GetProperty("properties").EnumerateObject()
                .Any(property => string.Equals(property.Name, nameof(SelectionRequest.TemplateKey), StringComparison.OrdinalIgnoreCase)));
        var parameterSchema = FindPropertySchema(schema.RootElement, nameof(SelectionRequest.TemplateKey));
        Assert.IsNotNull(parameterSchema);
        CollectionAssert.AreEqual(
            new[] { "python-exec", "csharp-exec" },
            parameterSchema.Value.GetProperty("enum").EnumerateArray().Select(item => item.GetString()).ToArray());
        StringAssert.Contains(parameterSchema.Value.GetProperty("description").GetString(), "Python Exec");
        StringAssert.Contains(parameterSchema.Value.GetProperty("description").GetString(), "python-exec");

        var result = function.InvokeAsync(new AIFunctionArguments
        {
            ["request"] = new SelectionRequest { TemplateKey = "csharp-exec" }
        }).GetAwaiter().GetResult();
        var resultText = result is JsonElement resultElement
            ? resultElement.GetString()
            : result?.ToString();
        Assert.AreEqual("csharp-exec", resultText);
    }

    [TestMethod]
    public void Create_BindsFlattenedRequestArguments()
    {
        var plugin = new RecordingPlugin();
        var method = typeof(RecordingPlugin).GetMethod(nameof(RecordingPlugin.Select));
        var function = AdminChatFunctionToolFactory.Create(
            method,
            plugin,
            "Xncf_RecordingPlugin_Select",
            "Selects a sandbox template.",
            new[]
            {
                new FunctionParameterInfo
                {
                    Name = nameof(SelectionRequest.TemplateKey),
                    ParameterType = ParameterType.DropDownList,
                    SelectionList = new SelectionList(
                        SelectionType.DropDownList,
                        new[]
                        {
                            new SelectionItem("python-exec", "Python Exec"),
                            new SelectionItem("jupyter-csharp", "JupyterLab C#")
                        })
                }
            });

        var result = function.InvokeAsync(new AIFunctionArguments
        {
            ["TemplateKey"] = "jupyter-csharp"
        }).GetAwaiter().GetResult();

        var resultText = result is JsonElement resultElement
            ? resultElement.GetString()
            : result?.ToString();
        Assert.AreEqual("jupyter-csharp", resultText);
    }

    [TestMethod]
    public async Task WorkflowTool_DeclaresInputAndOptionalWorkflowParameters()
    {
        var provider = new RecordingWorkflowProvider();
        var function = AdminChatWorkflowToolFactory.Create(
            provider,
            42,
            new WorkflowFunctionCallingDescriptor(
                7,
                "客户通知",
                "发送客户通知",
                new[]
                {
                    new WorkflowFunctionCallingParameter("customerId", "客户编号")
                }));

        using var schema = JsonDocument.Parse(function.JsonSchema.GetRawText());
        var properties = schema.RootElement.GetProperty("properties");

        Assert.IsTrue(properties.TryGetProperty("input", out _));
        Assert.IsTrue(properties.TryGetProperty("customerId", out _));
        CollectionAssert.Contains(
            schema.RootElement.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray(),
            "input");

        var result = await function.InvokeAsync(new AIFunctionArguments
        {
            ["input"] = "请发送通知",
            ["customerId"] = "C-001"
        });

        Assert.AreEqual("workflow-output", result);
        Assert.AreEqual(42, provider.AdminUserId);
        Assert.AreEqual(7, provider.WorkflowId);
        Assert.AreEqual("请发送通知", provider.Input);
        Assert.AreEqual("C-001", provider.Parameters["customerId"]?.ToString());
    }

    private sealed class RecordingWorkflowProvider : IWorkflowFunctionCallingProvider
    {
        public int AdminUserId { get; private set; }
        public int WorkflowId { get; private set; }
        public string Input { get; private set; }
        public IReadOnlyDictionary<string, object?> Parameters { get; private set; }

        public Task<IReadOnlyList<WorkflowFunctionCallingDescriptor>> GetAvailableAsync(
            int adminUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkflowFunctionCallingDescriptor>>(
                Array.Empty<WorkflowFunctionCallingDescriptor>());

        public Task<WorkflowFunctionCallingResult> ExecuteAsync(
            int workflowId,
            int adminUserId,
            string input,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            WorkflowId = workflowId;
            AdminUserId = adminUserId;
            Input = input;
            Parameters = parameters;
            return Task.FromResult(new WorkflowFunctionCallingResult(true, "workflow-output", null));
        }
    }

    private sealed class RecordingPlugin
    {
        public int InvocationCount { get; private set; }
        public string LastValue { get; private set; }

        public string Echo(string value)
        {
            InvocationCount++;
            LastValue = value;
            return value;
        }

        public string Select(SelectionRequest request) => request.TemplateKey;
    }

    private sealed class SelectionRequest
    {
        public string TemplateKey { get; set; }
    }

    private static IReadOnlyList<AIFunction> CreateWorkspaceTools(
        IWorkspaceClient client,
        bool allowWrite,
        bool allowCli = false)
    {
        var factory = typeof(AdminChatAiService).Assembly.GetType(
            "Senparc.Areas.Admin.Domain.Services.AdminChatWorkspaceToolFactory")!;
        return (IReadOnlyList<AIFunction>)factory.GetMethod("Create")!.Invoke(
            null,
            [client, allowWrite, allowCli, null])!;
    }

    private static JsonElement? FindPropertySchema(JsonElement schema, string name)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (schema.TryGetProperty("properties", out var properties) &&
            properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in properties.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }

                var nested = FindPropertySchema(property.Value, name);
                if (nested.HasValue)
                {
                    return nested;
                }
            }
        }

        return null;
    }
}

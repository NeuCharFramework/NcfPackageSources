/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminChatWorkspaceToolFactory.cs
    文件功能描述：后台对话工作区工具工厂
    
    
    创建标识：Senparc - 20261015
    
    修改标识：Senparc - 20261021
    修改描述：v0.11.1 为后台对话提供工作区工具

    修改标识：Senparc - 20261024
    修改描述：空查询时回退为目录列表，避免 AI 首次浏览 Workspace 失败

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

internal static class AdminChatWorkspaceToolFactory
{
    public static IReadOnlyList<AIFunction> Create(
        IWorkspaceClient client,
        bool allowWrite,
        bool allowCli)
    {
        var target = new WorkspaceToolTarget(client, allowWrite, allowCli);
        var functions = new List<AIFunction>
        {
            Create(target, nameof(WorkspaceToolTarget.ListAsync), "workspace_list", "列出当前 Workspace 内的文件和目录。"),
            Create(target, nameof(WorkspaceToolTarget.ReadAsync), "workspace_read", "读取当前 Workspace 内的文本文件。"),
            Create(target, nameof(WorkspaceToolTarget.SearchAsync), "workspace_search", "在当前 Workspace 中按文件名和文件内容搜索，并返回匹配文件、行号和预览；如果未提供查询条件，则返回指定目录的文件和目录列表。")
        };

        if (allowWrite)
        {
            functions.Add(Create(target, nameof(WorkspaceToolTarget.WriteAsync), "workspace_write", "在当前 Workspace 内写入文本文件。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.MkdirAsync), "workspace_mkdir", "在当前 Workspace 内创建目录。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.PatchAsync), "workspace_patch", "对当前 Workspace 内的文件应用精确文本补丁；匹配数量不符合预期时不会写入。"));
        }

        if (allowCli)
        {
            functions.Add(Create(
                target,
                nameof(WorkspaceToolTarget.ExecutePythonAsync),
                "workspace_execute_python",
                "在当前 Workspace 内执行 Python .py 文件。执行器优先使用 Workspace 内的 .venv，并返回真实退出码、标准输出和错误输出。"));
        }

        return functions;
    }

    private static AIFunction Create(object target, string methodName, string name, string description)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"Workspace tool method not found: {methodName}");
        return AIFunctionFactory.Create(method, target, name, description);
    }

    private sealed class WorkspaceToolTarget
    {
        private readonly IWorkspaceClient _client;
        private readonly bool _allowWrite;
        private readonly bool _allowCli;

        public WorkspaceToolTarget(IWorkspaceClient client, bool allowWrite, bool allowCli)
        {
            _client = client;
            _allowWrite = allowWrite;
            _allowCli = allowCli;
        }

        public async Task<string> ListAsync(string relativePath = "")
        {
            var entries = await _client.ListAsync(relativePath).ConfigureAwait(false);
            return JsonSerializer.Serialize(entries);
        }

        public async Task<string> ReadAsync(string relativePath)
        {
            var file = await _client.ReadFileAsync(relativePath).ConfigureAwait(false);
            return System.Text.Encoding.UTF8.GetString(file.Content);
        }

        public async Task<string> SearchAsync(
            string nameQuery = "",
            string contentQuery = "",
            string relativePath = "",
            bool caseSensitive = false,
            int maxResults = 100)
        {
            if (string.IsNullOrWhiteSpace(nameQuery)
                && string.IsNullOrWhiteSpace(contentQuery))
            {
                var entries = await _client.ListAsync(relativePath).ConfigureAwait(false);
                return JsonSerializer.Serialize(new
                {
                    mode = "list",
                    entries
                });
            }

            var results = await _client.SearchAsync(
                new WorkspaceSearchRequest(
                    nameQuery,
                    contentQuery,
                    relativePath,
                    caseSensitive,
                    maxResults)).ConfigureAwait(false);
            return JsonSerializer.Serialize(results);
        }

        public async Task<string> WriteAsync(string relativePath, string content)
        {
            EnsureWrite();
            await _client.WriteFileAsync(
                new WorkspaceWriteFileRequest(
                    relativePath,
                    System.Text.Encoding.UTF8.GetBytes(content))).ConfigureAwait(false);
            return "写入成功。";
        }

        public async Task<string> MkdirAsync(string relativePath)
        {
            EnsureWrite();
            await _client.CreateDirectoryAsync(relativePath).ConfigureAwait(false);
            return "目录创建成功。";
        }

        public async Task<string> PatchAsync(
            string relativePath,
            string oldText,
            string newText,
            int expectedReplacements = 1,
            string expectedSha256 = "")
        {
            EnsureWrite();
            var result = await _client.ApplyPatchAsync(
                new WorkspacePatchRequest(
                    relativePath,
                    oldText,
                    newText,
                    expectedReplacements,
                    expectedSha256)).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }

        public async Task<string> ExecutePythonAsync(
            string scriptPath,
            string[] arguments = null,
            string workingDirectory = "")
        {
            if (!_allowCli)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace Python 执行权限。");
            }

            var result = await _client.ExecutePythonAsync(
                new WorkspacePythonRequest(scriptPath, arguments ?? Array.Empty<string>(), workingDirectory)).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }

        private void EnsureWrite()
        {
            if (!_allowWrite)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace 写入权限。");
            }
        }
    }
}

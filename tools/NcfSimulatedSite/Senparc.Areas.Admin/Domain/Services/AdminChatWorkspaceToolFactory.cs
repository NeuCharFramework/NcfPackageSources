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
        IWorkspaceStorage storage,
        string workspaceId,
        bool allowWrite,
        bool allowCli)
    {
        var target = new WorkspaceToolTarget(storage, workspaceId, allowWrite, allowCli);
        var functions = new List<AIFunction>
        {
            Create(target, nameof(WorkspaceToolTarget.ListAsync), "workspace_list", "列出当前 Workspace 内的文件和目录。"),
            Create(target, nameof(WorkspaceToolTarget.ReadAsync), "workspace_read", "读取当前 Workspace 内的文本文件。")
        };

        if (allowWrite)
        {
            functions.Add(Create(target, nameof(WorkspaceToolTarget.WriteAsync), "workspace_write", "在当前 Workspace 内写入文本文件。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.MkdirAsync), "workspace_mkdir", "在当前 Workspace 内创建目录。"));
        }

        if (allowCli)
        {
            functions.Add(Create(target, nameof(WorkspaceToolTarget.ExecuteAsync), "workspace_execute_cli", "在当前 Workspace 内执行白名单 CLI 命令。"));
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
        private readonly IWorkspaceStorage _storage;
        private readonly string _workspaceId;
        private readonly bool _allowWrite;
        private readonly bool _allowCli;

        public WorkspaceToolTarget(IWorkspaceStorage storage, string workspaceId, bool allowWrite, bool allowCli)
        {
            _storage = storage;
            _workspaceId = workspaceId;
            _allowWrite = allowWrite;
            _allowCli = allowCli;
        }

        public async Task<string> ListAsync(string relativePath = "")
        {
            var entries = await _storage.ListAsync(_workspaceId, relativePath).ConfigureAwait(false);
            return JsonSerializer.Serialize(entries);
        }

        public Task<string> ReadAsync(string relativePath)
        {
            return _storage.ReadFileAsync(_workspaceId, relativePath);
        }

        public async Task<string> WriteAsync(string relativePath, string content)
        {
            EnsureWrite();
            await _storage.WriteFileAsync(_workspaceId, relativePath, content).ConfigureAwait(false);
            return "写入成功。";
        }

        public async Task<string> MkdirAsync(string relativePath)
        {
            EnsureWrite();
            await _storage.CreateDirectoryAsync(_workspaceId, relativePath).ConfigureAwait(false);
            return "目录创建成功。";
        }

        public async Task<string> ExecuteAsync(string command, string[] arguments, string workingDirectory = "")
        {
            if (!_allowCli)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace CLI 权限。");
            }

            var result = await _storage.ExecuteCommandAsync(
                _workspaceId,
                new WorkspaceCommandRequest(command, arguments ?? Array.Empty<string>(), workingDirectory)).ConfigureAwait(false);
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

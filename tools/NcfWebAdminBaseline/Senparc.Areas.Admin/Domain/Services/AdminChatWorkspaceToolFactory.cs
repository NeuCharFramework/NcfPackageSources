/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatWorkspaceToolFactory.cs
    File description: Related implementation.

    Created by: Senparc - 20261015

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261024
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261111
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.CO2NET.Trace;
using Senparc.Xncf.Workspace.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;

namespace Senparc.Areas.Admin.Domain.Services;

internal static class AdminChatWorkspaceToolFactory
{
    /// <summary>
    /// Creates the read, document, write, delete, and optional Python tools exposed
    /// to an AdminChat session according to its capabilities.
    /// </summary>
    /// <param name="client">The workspace client bound to the current workspace.</param>
    /// <param name="allowWrite">Whether file and directory mutation tools are allowed.</param>
    /// <param name="allowCli">Whether Python execution is allowed.</param>
    /// <param name="documents">Optional document reader enabling document-specific tools.</param>
    /// <returns>The diagnostic-wrapped AI function collection.</returns>
    public static IReadOnlyList<AIFunction> Create(
        IWorkspaceClient client,
        bool allowWrite,
        bool allowCli,
        WorkspaceDocumentReader documents = null)
    {
        var budget = new WorkspaceToolBudget();
        var target = new WorkspaceToolTarget(client, allowWrite, allowCli, budget);
        var functions = new List<AIFunction>
        {
            Create(target, nameof(WorkspaceToolTarget.ListAsync), "workspace_list", "列出当前 Workspace 内的文件和目录。relativePath 可省略或为空，此时表示 Workspace 根目录。"),
            Create(target, nameof(WorkspaceToolTarget.ReadAsync), "workspace_read", "读取当前 Workspace 内的文本文件。relativePath 必须指向具体文件；每次最多返回 4000 字符。"),
            Create(
                target,
                nameof(WorkspaceToolTarget.SearchAsync),
                "workspace_search",
                "在当前 Workspace 中搜索文件名、相对路径 glob 和文件内容。relativePath 可省略或为空，此时表示 Workspace 根目录。nameQuery 是文件名子串；pathGlob 支持相对路径 glob，例如 tools/NcfWebAdminBaseline/**/*Workspace*；contentQuery 默认是字面量，设置 contentRegex=true 后支持正则表达式和 | 批量条件，例如 WorkspaceAssets|sandbox|Content-Security-Policy。返回匹配文件、行号和预览；如果未提供查询条件，则返回指定目录的文件和目录列表。")
        };
        if (documents != null)
        {
            var reader = new WorkspaceDocumentTools(client, documents, budget);
            functions.Add(Create(reader, nameof(WorkspaceDocumentTools.InfoAsync), "workspace_document_info", "获取 PDF/Word/Excel/PPT/文本文件的本地文字层分段目录、工作表名称和解析限制；不会调用 OCR 或视觉模型。"));
            functions.Add(Create(reader, nameof(WorkspaceDocumentTools.ReadAsync), "workspace_document_read", "按分段（PDF 页/幻灯片/工作表/正文块）和字符偏移读取资料，每次最多 4000 字符，使用 nextOffset 分页，不能将部分内容声称为全文。"));
            functions.Add(Create(reader, nameof(WorkspaceDocumentTools.SearchAsync), "workspace_document_search", "在本地解析的文档中搜索关键词，最多返回 10 个命中分段及短预览，再按需读取对应分段。"));
        }

        if (allowWrite)
        {
            functions.Add(Create(target, nameof(WorkspaceToolTarget.WriteAsync), "workspace_write", "在当前 Workspace 内写入文本文件。relativePath 必须是非空的文件相对路径；内容较长或传输失败时，应先读取现有文件并使用 workspace_patch 分段追加。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.MkdirAsync), "workspace_mkdir", "在当前 Workspace 内创建目录。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.PatchAsync), "workspace_patch", "对当前 Workspace 内的文件应用精确文本补丁；匹配数量不符合预期时不会写入。"));
            functions.Add(Create(target, nameof(WorkspaceToolTarget.DeleteAsync), "workspace_delete", "删除当前 Workspace 内的文件或空目录；删除非空目录时必须明确使用 recursive=true。"));
        }

        if (allowCli)
        {
            functions.Add(Create(
                target,
                nameof(WorkspaceToolTarget.ExecutePythonAsync),
                "workspace_execute_python",
                "在当前 Workspace 内执行 Python .py 文件。执行器优先使用 Workspace 内的 .venv，并返回真实退出码、标准输出和错误输出。"));
        }

        return functions
            .Select(function => (AIFunction)new WorkspaceDiagnosticAIFunction(function, client.WorkspaceId))
            .ToList();
    }

    /// <summary>
    /// Reflects a public target method into an AI function with the supplied tool metadata.
    /// </summary>
    /// <param name="target">The object containing the instance method.</param>
    /// <param name="methodName">The reflected method name.</param>
    /// <param name="name">The AI tool name.</param>
    /// <param name="description">The AI tool description.</param>
    /// <returns>The constructed AI function.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the target method cannot be found.</exception>
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
        private readonly WorkspaceToolBudget _budget;

        public WorkspaceToolTarget(IWorkspaceClient client, bool allowWrite, bool allowCli, WorkspaceToolBudget budget)
        {
            _client = client;
            _allowWrite = allowWrite;
            _allowCli = allowCli;
            _budget = budget;
        }

        /// <summary>
        /// Lists a paged window of workspace directory entries within the output budget.
        /// </summary>
        /// <param name="relativePath">The workspace-relative directory path.</param>
        /// <param name="offset">The zero-based entry offset.</param>
        /// <param name="maxEntries">The requested page size, capped at 100.</param>
        /// <param name="cancellationToken">Token used to cancel the workspace query.</param>
        /// <returns>Budgeted JSON entries and continuation metadata.</returns>
        public async Task<string> ListAsync(string relativePath = "", int offset = 0, int maxEntries = 30,
            CancellationToken cancellationToken = default)
        {
            if (offset < 0 || maxEntries is < 1 or > 100) throw new ArgumentException("目录分页参数无效，最多返回 100 个条目。");
            relativePath ??= string.Empty;
            var entries = await _client.ListAsync(relativePath, cancellationToken).ConfigureAwait(false);
            return _budget.Json(new { entries = entries.Skip(offset).Take(maxEntries), nextOffset = offset + maxEntries,
                hasMore = offset + maxEntries < entries.Count });
        }

        /// <summary>
        /// Reads a bounded UTF-8 text window from a workspace file and returns its ETag
        /// and continuation position.
        /// </summary>
        /// <param name="relativePath">The workspace-relative file path.</param>
        /// <param name="offset">The zero-based character offset.</param>
        /// <param name="maxCharacters">The requested character count, capped at 4000.</param>
        /// <param name="cancellationToken">Token used to cancel the read.</param>
        /// <returns>Budgeted JSON containing text and continuation metadata.</returns>
        public async Task<string> ReadAsync(string relativePath, int offset = 0, int maxCharacters = 4000,
            CancellationToken cancellationToken = default)
        {
            EnsureFilePath(relativePath);
            if (offset < 0 || maxCharacters < 1) throw new ArgumentException("文本读取参数无效，offset 必须非负，maxCharacters 必须大于 0。");
            maxCharacters = Math.Min(maxCharacters, 4000);
            var extension = Path.GetExtension(relativePath).ToLowerInvariant();
            if (extension is ".pdf" or ".docx" or ".xlsx" or ".pptx" or ".doc" or ".xls" or ".ppt"
                or ".jpg" or ".jpeg" or ".png" or ".webp" or ".zip")
                throw new ArgumentException("不能将二进制文件作为 UTF-8 文本读取。文档请使用 workspace_document_info/read；图片必须由用户明确启用视觉处理。");
            var file = await _client.ReadFileAsync(relativePath, cancellationToken: cancellationToken).ConfigureAwait(false);
            var text = new System.Text.UTF8Encoding(false, true).GetString(file.Content);
            if (text.Contains('\0')) throw new ArgumentException("文件包含二进制内容，不能作为文本读取。");
            if (offset > text.Length) throw new ArgumentException("offset 超过文件长度。");
            var count = Math.Min(maxCharacters, text.Length - offset);
            if (count > 0 && offset + count < text.Length && char.IsHighSurrogate(text[offset + count - 1])) count--;
            return _budget.Json(new { content = text.Substring(offset, count), etag = file.Metadata.ETag,
                nextOffset = offset + count, hasMore = offset + count < text.Length });
        }

        /// <summary>
        /// Searches workspace entries using name, path-glob, and optional content criteria;
        /// with no criteria it returns the requested directory listing.
        /// </summary>
        /// <param name="nameQuery">Optional file-name substring.</param>
        /// <param name="contentQuery">Optional content query.</param>
        /// <param name="relativePath">The workspace-relative root directory.</param>
        /// <param name="caseSensitive">Whether text matching is case-sensitive.</param>
        /// <param name="maxResults">The maximum result count.</param>
        /// <param name="pathGlob">Optional relative-path glob.</param>
        /// <param name="contentRegex">Whether the content query is a regular expression.</param>
        /// <returns>Budgeted JSON search results.</returns>
        public async Task<string> SearchAsync(
            string nameQuery = "",
            string contentQuery = "",
            string relativePath = "",
            bool caseSensitive = false,
            int maxResults = 100,
            string pathGlob = "",
            bool contentRegex = false)
        {
            if (maxResults is < 1 or > 100) throw new ArgumentException("文件搜索最多返回 100 个结果。");
            relativePath ??= string.Empty;
            if (string.IsNullOrWhiteSpace(nameQuery)
                && string.IsNullOrWhiteSpace(contentQuery)
                && string.IsNullOrWhiteSpace(pathGlob))
            {
                return await ListAsync(relativePath).ConfigureAwait(false);
            }

            var results = await _client.SearchAsync(
                new WorkspaceSearchRequest(
                    nameQuery,
                    contentQuery,
                    relativePath,
                    caseSensitive,
                    maxResults,
                    PathGlob: pathGlob,
                    ContentRegex: contentRegex)).ConfigureAwait(false);
            return _budget.Json(results);
        }

        /// <summary>
        /// Writes UTF-8 text to a workspace file after checking write capability and
        /// returns a structured before/after change summary.
        /// </summary>
        /// <param name="relativePath">The workspace-relative file path.</param>
        /// <param name="content">The replacement text content.</param>
        /// <returns>JSON describing the write and resulting change.</returns>
        public async Task<string> WriteAsync(string relativePath, string content)
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            ArgumentNullException.ThrowIfNull(content);
            var before = await SnapshotAsync(relativePath).ConfigureAwait(false);
            var result = await _client.WriteFileAsync(
                new WorkspaceWriteFileRequest(
                    relativePath,
                    System.Text.Encoding.UTF8.GetBytes(content))).ConfigureAwait(false);
            var after = await SnapshotAsync(relativePath).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "写入成功。",
                change = CreateChange(
                    relativePath,
                    result.Created ? "added" : "modified",
                    before,
                    after)
            });
        }

        /// <summary>
        /// Creates a workspace directory after checking write capability.
        /// </summary>
        /// <param name="relativePath">The workspace-relative directory path.</param>
        /// <returns>A success message.</returns>
        public async Task<string> MkdirAsync(string relativePath)
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            await _client.CreateDirectoryAsync(relativePath).ConfigureAwait(false);
            return "目录创建成功。";
        }

        /// <summary>
        /// Applies an optimistic exact-text patch and returns either the rejection
        /// result or a structured before/after change summary.
        /// </summary>
        /// <param name="relativePath">The workspace-relative file path.</param>
        /// <param name="oldText">The expected text to replace.</param>
        /// <param name="newText">The replacement text.</param>
        /// <param name="expectedReplacements">The exact replacement count required.</param>
        /// <param name="expectedSha256">Optional expected file fingerprint.</param>
        /// <returns>JSON describing patch success or failure.</returns>
        public async Task<string> PatchAsync(
            string relativePath,
            string oldText,
            string newText,
            int expectedReplacements = 1,
            string expectedSha256 = "")
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            var before = await SnapshotAsync(relativePath).ConfigureAwait(false);
            var result = await _client.ApplyPatchAsync(
                new WorkspacePatchRequest(
                    relativePath,
                    oldText,
                    newText,
                    expectedReplacements,
                    expectedSha256)).ConfigureAwait(false);
            if (!result.Applied)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    result
                });
            }

            var after = await SnapshotAsync(relativePath).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                success = true,
                result,
                change = CreateChange(relativePath, "modified", before, after)
            });
        }

        /// <summary>
        /// Deletes a workspace file or directory after checking write capability,
        /// optional recursive behavior, and an optional expected ETag.
        /// </summary>
        /// <param name="relativePath">The workspace-relative target path.</param>
        /// <param name="recursive">Whether a directory may be non-empty.</param>
        /// <param name="expectedETag">Optional expected target fingerprint.</param>
        /// <returns>JSON describing the deletion and prior state.</returns>
        public async Task<string> DeleteAsync(
            string relativePath,
            bool recursive = false,
            string expectedETag = "")
        {
            EnsureWrite();
            EnsureFilePath(relativePath);
            var before = await SnapshotAsync(relativePath).ConfigureAwait(false);
            await _client.DeleteEntryAsync(
                new WorkspaceDeleteEntryRequest(relativePath, recursive, expectedETag)).ConfigureAwait(false);
            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "删除成功。",
                change = CreateChange(relativePath, "deleted", before, WorkspaceFileSnapshot.Missing)
            });
        }

        /// <summary>
        /// Executes a workspace Python script only when CLI capability is enabled.
        /// </summary>
        /// <param name="scriptPath">The workspace-relative Python script path.</param>
        /// <param name="arguments">Optional script arguments.</param>
        /// <param name="workingDirectory">Optional workspace-relative working directory.</param>
        /// <returns>Serialized execution status, output, and exit information.</returns>
        public async Task<string> ExecutePythonAsync(
            string scriptPath,
            string[] arguments = null,
            string workingDirectory = "")
        {
            if (!_allowCli)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace Python 执行权限。");
            }

            EnsureFilePath(scriptPath);
            workingDirectory ??= string.Empty;
            var result = await _client.ExecutePythonAsync(
                new WorkspacePythonRequest(scriptPath, arguments ?? Array.Empty<string>(), workingDirectory)).ConfigureAwait(false);
            return JsonSerializer.Serialize(result);
        }

        /// <summary>
        /// Rejects mutation requests when the current session lacks write capability.
        /// </summary>
        private void EnsureWrite()
        {
            if (!_allowWrite)
            {
                throw new InvalidOperationException("当前会话未启用 Workspace 写入权限。");
            }
        }

        /// <summary>
        /// Ensures a file-oriented tool received a non-empty relative path.
        /// </summary>
        /// <param name="relativePath">The path supplied to the tool.</param>
        /// <exception cref="ArgumentException">Thrown when the path is blank.</exception>
        private static void EnsureFilePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(
                    "relativePath 不能为空；即使在 Workspace 根目录写文件，也必须提供文件名，例如 index.html。",
                    nameof(relativePath));
            }
        }

        /// <summary>
        /// Captures file existence, UTF-8 text, and byte length before or after a mutation.
        /// Binary, oversized, or undecodable files retain metadata without text content.
        /// </summary>
        /// <param name="relativePath">The workspace-relative file path.</param>
        /// <returns>A snapshot suitable for change reporting.</returns>
        private async Task<WorkspaceFileSnapshot> SnapshotAsync(string relativePath)
        {
            try
            {
                var metadata = await _client.GetFileInfoAsync(relativePath).ConfigureAwait(false);
                if (metadata.IsDirectory)
                {
                    throw new IOException("Workspace file target is a directory.");
                }

                try
                {
                    var file = await _client.ReadFileAsync(relativePath).ConfigureAwait(false);
                    return new WorkspaceFileSnapshot(
                        true,
                        new UTF8Encoding(false, true).GetString(file.Content),
                        file.Metadata.Length);
                }
                catch (WorkspaceLimitException)
                {
                    return new WorkspaceFileSnapshot(true, null, metadata.Length);
                }
                catch (DecoderFallbackException)
                {
                    return new WorkspaceFileSnapshot(true, null, metadata.Length);
                }
            }
            catch (Exception exception) when (
                exception is FileNotFoundException or DirectoryNotFoundException)
            {
                return WorkspaceFileSnapshot.Missing;
            }
        }

        /// <summary>
        /// Builds a structured change summary including byte sizes and optional line deltas.
        /// </summary>
        /// <param name="relativePath">The changed workspace path.</param>
        /// <param name="changeType">The change category.</param>
        /// <param name="before">The pre-operation snapshot.</param>
        /// <param name="after">The post-operation snapshot.</param>
        /// <returns>An anonymous change summary object for JSON serialization.</returns>
        private static object CreateChange(
            string relativePath,
            string changeType,
            WorkspaceFileSnapshot before,
            WorkspaceFileSnapshot after)
        {
            var diff = (!before.Exists || before.Text != null)
                && (!after.Exists || after.Text != null)
                ? CountLineChanges(before.Exists ? before.Text : string.Empty, after.Exists ? after.Text : string.Empty)
                : ((int Added, int Removed, bool Exact)?)null;
            return new
            {
                path = relativePath,
                changeType,
                beforeBytes = before.Exists ? before.Length : 0,
                afterBytes = after.Exists ? after.Length : 0,
                addedLines = diff?.Added,
                removedLines = diff?.Removed,
                lineCountsExact = diff?.Exact
            };
        }

        /// <summary>
        /// Estimates added and removed line counts using a bounded longest-common-
        /// subsequence calculation, falling back to inexact counts for large files.
        /// </summary>
        /// <param name="before">The previous text.</param>
        /// <param name="after">The resulting text.</param>
        /// <returns>Added lines, removed lines, and whether the counts are exact.</returns>
        private static (int Added, int Removed, bool Exact) CountLineChanges(
            string before,
            string after)
        {
            var beforeLines = SplitLines(before);
            var afterLines = SplitLines(after);
            if (beforeLines.Length > 5000 || afterLines.Length > 5000)
            {
                return (afterLines.Length, beforeLines.Length, false);
            }

            var next = new int[afterLines.Length + 1];
            var current = new int[afterLines.Length + 1];
            for (var beforeIndex = beforeLines.Length - 1; beforeIndex >= 0; beforeIndex--)
            {
                current[afterLines.Length] = 0;
                for (var afterIndex = afterLines.Length - 1; afterIndex >= 0; afterIndex--)
                {
                    current[afterIndex] = string.Equals(
                        beforeLines[beforeIndex],
                        afterLines[afterIndex],
                        StringComparison.Ordinal)
                        ? next[afterIndex + 1] + 1
                        : Math.Max(next[afterIndex], current[afterIndex + 1]);
                }

                (next, current) = (current, next);
            }

            var commonLines = next[0];
            return (afterLines.Length - commonLines, beforeLines.Length - commonLines, true);
        }

        /// <summary>
        /// Splits text into logical lines while normalizing CRLF and CR separators.
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <returns>Logical lines without a trailing empty line.</returns>
        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<string>();
            }

            var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
            return lines.Length > 0 && lines[^1].Length == 0
                ? lines[..^1]
                : lines;
        }

        private sealed record WorkspaceFileSnapshot(bool Exists, string Text, long Length)
        {
            public static WorkspaceFileSnapshot Missing { get; } = new(false, null, 0);
        }
    }

    private sealed class WorkspaceDiagnosticAIFunction : DelegatingAIFunction
    {
        private readonly string _workspaceId;

        public WorkspaceDiagnosticAIFunction(AIFunction innerFunction, string workspaceId)
            : base(innerFunction)
        {
            _workspaceId = workspaceId ?? string.Empty;
        }

        /// <summary>
        /// Normalizes root-capable arguments, invokes the wrapped workspace tool,
        /// and converts expected argument/authorization failures into structured diagnostics.
        /// </summary>
        /// <param name="arguments">The AI tool arguments.</param>
        /// <param name="cancellationToken">Token used to cancel invocation.</param>
        /// <returns>The wrapped tool result or serialized diagnostic failure.</returns>
        protected override async ValueTask<object> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var argumentSummary = FormatArguments(arguments);
            try
            {
                var normalizedArguments = NormalizeRootPath(arguments);
                return await base.InvokeCoreAsync(normalizedArguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var root = exception.GetBaseException();
                if (root is ArgumentException or UnauthorizedAccessException)
                {
                    var rejected =
                        $"WorkspaceId={_workspaceId}; Tool={Name}; Arguments={argumentSummary}; " +
                        $"Rejected={root.GetType().FullName}: {root.Message}";
                    SenparcTrace.SendCustomLog("AdminChat.WorkspaceTool.Rejected", rejected);
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = root.Message,
                        errorType = root.GetType().Name,
                        requiredArguments = GetRequiredArgumentNames(),
                        retryable = false
                    });
                }

                var detail =
                    $"WorkspaceId={_workspaceId}; Tool={Name}; Arguments={argumentSummary}; " +
                    $"Error={root.GetType().FullName}: {root.Message}";
                SenparcTrace.SendCustomLog("AdminChat.WorkspaceTool.Failed", detail);
                SenparcTrace.BaseExceptionLog(root);
                Console.Error.WriteLine($"[AdminChat.WorkspaceTool.Failed] {detail}");
                throw;
            }
        }

        /// <summary>
        /// Returns required argument names used when a workspace tool rejects a request.
        /// </summary>
        /// <returns>The tool-specific required argument names.</returns>
        private string[] GetRequiredArgumentNames() => Name switch
        {
            "workspace_read" or "workspace_mkdir" or "workspace_delete" => ["relativePath"],
            "workspace_write" => ["relativePath", "content"],
            "workspace_patch" => ["relativePath", "oldText", "newText"],
            "workspace_document_info" => ["relativePath"],
            "workspace_document_read" or "workspace_document_search" => ["relativePath"],
            "workspace_execute_python" => ["scriptPath"],
            _ => []
        };

        /// <summary>
        /// Ensures root-capable list/search tools receive an explicit empty root path
        /// when the caller omitted it.
        /// </summary>
        /// <param name="arguments">The original AI function arguments.</param>
        /// <returns>Normalized arguments, or the original instance when no change is needed.</returns>
        private AIFunctionArguments NormalizeRootPath(AIFunctionArguments arguments)
        {
            if ((Name != "workspace_list" && Name != "workspace_search") || arguments == null)
            {
                return arguments;
            }

            var values = arguments.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
            if (!values.TryGetValue("relativePath", out var relativePath) || relativePath == null)
            {
                values["relativePath"] = string.Empty;
            }

            return new AIFunctionArguments(values)
            {
                Services = arguments.Services,
                Context = arguments.Context
            };
        }

        /// <summary>
        /// Formats tool arguments for diagnostics without dumping full file content.
        /// </summary>
        /// <param name="arguments">The argument collection.</param>
        /// <returns>A bounded diagnostic string.</returns>
        private static string FormatArguments(IEnumerable<KeyValuePair<string, object>> arguments)
        {
            if (arguments == null)
            {
                return "(none)";
            }

            var values = arguments
                .Select(pair => $"{pair.Key}={FormatArgumentValue(pair.Key, pair.Value)}")
                .ToList();
            return values.Count == 0 ? "(none)" : string.Join(", ", values);
        }

        /// <summary>
        /// Formats one argument value while redacting large content fields and line breaks.
        /// </summary>
        /// <param name="key">The argument name.</param>
        /// <param name="value">The argument value.</param>
        /// <returns>A bounded diagnostic representation.</returns>
        private static string FormatArgumentValue(string key, object value)
        {
            if (key.Contains("content", StringComparison.OrdinalIgnoreCase))
            {
                return value is string text
                    ? $"[text length={text.Length}]"
                    : $"[{value?.GetType().Name ?? "null"}]";
            }

            var textValue = value?.ToString() ?? "null";
            textValue = textValue.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return textValue.Length <= 500 ? textValue : textValue[..500] + "...";
        }
    }
}

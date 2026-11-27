/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceDocumentTools.cs
    File description: WorkspaceDocumentTools related definitions and implementation

    Created by: Senparc - 20261026

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Domain.Services;

internal sealed class WorkspaceToolBudget
{
    public const int MaxCharacters = 16000;
    private int _used;
    /// <summary>
    /// Serializes a tool result while atomically enforcing the per-turn character budget.
    /// </summary>
    /// <param name="value">The value to serialize as JSON.</param>
    /// <returns>The serialized JSON text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result would exceed the turn budget.</exception>
    public string Json(object value)
    {
        var text = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        while (true)
        {
            var previous = Volatile.Read(ref _used);
            if (text.Length > MaxCharacters - previous)
                throw new InvalidOperationException("本轮 Workspace 资料输出已达 16000 字符限额。请停止继续读取，明确告知资料尚未读完，或请用户缩小范围／下一轮继续。");
            if (Interlocked.CompareExchange(ref _used, previous + text.Length, previous) == previous) return text;
        }
    }
}

internal sealed class WorkspaceDocumentTools
{
    private readonly IWorkspaceClient _client;
    private readonly WorkspaceDocumentReader _reader;
    private readonly WorkspaceToolBudget _budget;
    /// <summary>
    /// Initializes workspace document tools with the client, document reader, and output budget.
    /// </summary>
    /// <param name="client">The workspace client used to access document data.</param>
    /// <param name="reader">The reader that sections workspace documents.</param>
    /// <param name="budget">The per-turn output budget.</param>
    public WorkspaceDocumentTools(IWorkspaceClient client, WorkspaceDocumentReader reader, WorkspaceToolBudget budget)
    {
        _client = client;
        _reader = reader;
        _budget = budget;
    }

    /// <summary>
    /// Returns document metadata, section titles, and a bounded section summary.
    /// </summary>
    /// <param name="relativePath">The workspace-relative document path.</param>
    /// <param name="expectedETag">The optional expected version tag.</param>
    /// <param name="cancellationToken">The cancellation token for the workspace read.</param>
    /// <returns>A budgeted JSON metadata response.</returns>
    public async Task<string> InfoAsync(string relativePath, string expectedETag = "", CancellationToken cancellationToken = default)
    {
        var (document, etag) = await _reader.ReadAsync(_client, relativePath, expectedETag, cancellationToken);
        return _budget.Json(new
        {
            path = relativePath, etag, document.Notice, document.Characters,
            sectionCount = document.Sections.Count,
            sections = document.Sections.Take(20).Select((section, index) => new
            {
                section = index + 1, title = section.Title, characters = section.Text.Length
            }),
            sectionIndexTruncated = document.Sections.Count > 20,
            hasText = document.HasText
        });
    }

    /// <summary>
    /// Reads a bounded character window from one document section and reports continuation metadata.
    /// </summary>
    /// <param name="relativePath">The workspace-relative document path.</param>
    /// <param name="section">The one-based section number.</param>
    /// <param name="offset">The zero-based character offset within the section.</param>
    /// <param name="maxCharacters">The requested window size, limited to 4000 characters.</param>
    /// <param name="expectedETag">The optional expected version tag.</param>
    /// <param name="cancellationToken">The cancellation token for the workspace read.</param>
    /// <returns>A budgeted JSON text window response.</returns>
    /// <exception cref="ArgumentException">Thrown for invalid section, offset, or window arguments.</exception>
    public async Task<string> ReadAsync(string relativePath, int section = 1, int offset = 0,
        int maxCharacters = 4000, string expectedETag = "", CancellationToken cancellationToken = default)
    {
        if (maxCharacters is < 1 or > 4000 || offset < 0 || section < 1)
            throw new ArgumentException("读取参数无效：section 从 1 开始，offset 非负，maxCharacters 为 1–4000。");
        var (document, etag) = await _reader.ReadAsync(_client, relativePath, expectedETag, cancellationToken);
        if (section > document.Sections.Count) throw new ArgumentException("分段超出文档范围，请先调用 workspace_document_info。");
        var part = document.Sections[section - 1];
        if (offset > part.Text.Length) throw new ArgumentException("offset 超过分段长度。");
        var length = Math.Min(maxCharacters, part.Text.Length - offset);
        if (length > 0 && offset + length < part.Text.Length && char.IsHighSurrogate(part.Text[offset + length - 1])) length--;
        return _budget.Json(new
        {
            path = relativePath, etag, section, title = part.Title, offset,
            text = part.Text.Substring(offset, length),
            nextOffset = offset + length, hasMore = offset + length < part.Text.Length,
            sectionCount = document.Sections.Count, document.Notice
        });
    }

    /// <summary>
    /// Searches document sections case-insensitively and returns up to ten bounded snippets.
    /// </summary>
    /// <param name="relativePath">The workspace-relative document path.</param>
    /// <param name="query">The non-empty search text, limited to 200 characters.</param>
    /// <param name="expectedETag">The optional expected version tag.</param>
    /// <param name="cancellationToken">The cancellation token for the workspace read.</param>
    /// <returns>A budgeted JSON search response.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is empty or too long.</exception>
    public async Task<string> SearchAsync(string relativePath, string query, string expectedETag = "",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 200)
            throw new ArgumentException("文档搜索关键词必须为 1–200 个字符。");
        var (document, etag) = await _reader.ReadAsync(_client, relativePath, expectedETag, cancellationToken);
        var matches = new List<object>();
        for (var index = 0; index < document.Sections.Count && matches.Count < 10; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var part = document.Sections[index];
            var offset = part.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (offset < 0) continue;
            var start = Math.Max(0, offset - 80);
            matches.Add(new
            {
                section = index + 1, title = part.Title, matchOffset = offset, snippetOffset = start,
                snippet = part.Text.Substring(start, Math.Min(240, part.Text.Length - start))
            });
        }
        return _budget.Json(new { path = relativePath, etag, matches, limit = 10, document.Notice });
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMemoryService.cs
    File description: AdminChatMemoryService related definitions and implementation

    Created by: Senparc - 20261103

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261104
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261105
    Change description: v0.12.26 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.PromptRange.Abstractions.Memory;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record AdminChatMemoryContext(int TenantId, int UserId, int SessionId,
    AdminChatMemoryProfile Profile, bool Enabled, string Instructions,
    AdminChatMemoryMode Mode = AdminChatMemoryMode.Learn, string WorkspaceId = "", int? SourceMessageId = null)
{
    public IReadOnlyList<AdminChatMemorySource> RecentMessages { get; init; } = Array.Empty<AdminChatMemorySource>();
}

public sealed record AdminChatMemorySource(int MessageId, string Role, string Text);
public sealed record AdminChatMemoryProposal(AdminChatMemoryEntry Entry, bool Remove);

public sealed record AdminChatMemoryView(string Revision, string Markdown, string SystemMessage,
    bool Enabled, int EntryCount, int ContextByteBudget)
{
    public AdminChatMemoryMode Mode { get; init; }
    public string WorkspaceId { get; init; }
    public IReadOnlyList<AdminChatMemoryEntry> Entries { get; init; }
    public IReadOnlyList<AdminChatMemoryRevision> History { get; init; }
    public int TokenBudget => AdminChatMemoryRetrieval.TokenBudget;
}

public sealed record AdminChatMemoryEdit(string Id, int Version, string Text, string Topic, string Tier,
    string Kind, bool Pinned, string Scope, DateTimeOffset? ExpiresAt, bool Remove);

public sealed record AdminChatMemoryUpdateResult(string Status, string Reason, bool Requested,
    int GlobalChanges, int WorkspaceChanges, string PromptCode)
{
    public string Revision { get; init; }
    public IReadOnlyList<AdminChatMemoryProposal> Proposals { get; init; } = Array.Empty<AdminChatMemoryProposal>();
}

public sealed class AdminChatMemoryService
{
    private readonly IAdminChatMemoryStore _store;
    private readonly AdminChatSessionService _sessions;
    private readonly IServiceProvider _services;
    private readonly ILogger<AdminChatMemoryService> _logger;
    public string UpdateStatus { get; private set; } = "disabled";
    public AdminChatMemoryUsage Usage { get; private set; }
    private string _updateReason = "mode-off";
    private bool _updateRequested;
    private int _globalChanges;
    private int _workspaceChanges;
    private string _promptCode;
    private string _proposalRevision;
    private IReadOnlyList<AdminChatMemoryProposal> _proposals = Array.Empty<AdminChatMemoryProposal>();
    public AdminChatMemoryUpdateResult UpdateResult =>
        new(UpdateStatus, _updateReason, _updateRequested, _globalChanges, _workspaceChanges, _promptCode)
        { Revision = _proposalRevision, Proposals = _proposals };

    /// <summary>
    /// Builds the system instruction block that explains memory persistence scope,
    /// learning modes, review boundaries, and the requirement to wait for a system receipt.
    /// </summary>
    /// <param name="mode">The current memory mode.</param>
    /// <returns>The instruction text appended to the chat system context.</returns>
    public static string StorageInstructions(AdminChatMemoryMode mode) =>
        "\n[Memory persistence contract]\nCurrent Memory mode: " + mode + ". "
        + "Long-term Memory is private per-user database storage, not a Workspace Memory.md file. "
        + "Workspace files named Memory.md are not automatically imported. "
        + "In Learn mode a separate service may extract and validate explicitly requested facts (including ordinary "
        + "names, nicknames and characteristics) and preferences AFTER your reply. "
        + "Do not refuse ordinary person facts merely because they are not work preferences. "
        + "In Learn mode an explicit self-contained remember request already authorizes extraction; "
        + "do not ask again whether the user wants long-term Memory. Report no success before the system receipt. "
        + "A present request such as '请记录', '立即保存' or 'save it now' can refer to facts the user supplied "
        + "in recent messages. The service resolves that reference after your reply; user-stated facts may save "
        + "automatically after validation. Only changes relying on assistant-generated content require UI review. "
        + "A future announcement does not authorize saving each following message before the user asks to save. "
        + "You cannot click the review UI. No candidate result has been issued to you: never claim candidates "
        + "exist or direct the user to a supposed review screen. Acknowledge the request and let the actual system "
        + "receipt show any available review action. Do not require redundant confirmation for user-stated facts. "
        + "You have not received its save result. Never claim a fact or preference has been recorded, saved or updated "
        + "in long-term Memory or in a file based only on the user's request or your intention. "
        + "You may acknowledge the request for this conversation, explaining that the system's Memory update report "
        + "is the persistence confirmation. Off and ReadOnly modes never learn automatically. "
        + "Do not invent a timestamp or a format the user did not specify; use a trusted time tool when available "
        + "and disclose when real-time access is unavailable.\n";

    public AdminChatMemoryService(IAdminChatMemoryStore store, AdminChatSessionService sessions,
        IServiceProvider services, ILogger<AdminChatMemoryService> logger)
    {
        _store = store;
        _sessions = sessions;
        _services = services;
        _logger = logger;
    }

    private int TenantId
    {
        get
        {
            if (!SiteConfig.SenparcCoreSetting.EnableMultiTenant) return 0;
            var tenant = RequestTenantSnapshot.Capture(_services);
            if (!tenant.MatchSuccess || tenant.Id <= 0) throw new NcfExceptionBase("Memory requires a valid tenant.");
            return tenant.Id;
        }
    }

    /// <summary>
    /// Validates the administrator and resolves an owner-scoped chat session.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The chat session identifier, or zero for global memory operations.</param>
    /// <returns>The authorized session, or <see langword="null"/> for session zero.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when authentication or session access fails.</exception>
    private async Task<AdminChatSession> AuthorizeAsync(int userId, int sessionId)
    {
        if (userId <= 0) throw new NcfExceptionBase("Authentication required.");
        if (sessionId < 0) throw new NcfExceptionBase("Invalid Chat session.");
        return sessionId == 0 ? null : await _sessions.GetSessionByIdAsync(sessionId, userId)
            ?? throw new NcfExceptionBase("Chat session not found or access denied.");
    }

    /// <summary>
    /// Reads the owner-scoped memory profile and projects entries/history visible
    /// to the selected session workspace.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <returns>The memory view for the session.</returns>
    public async Task<AdminChatMemoryView> GetAsync(int userId, int sessionId)
    {
        var session = await AuthorizeAsync(userId, sessionId);
        return View(await _store.ReadAsync(TenantId, userId), sessionId, session?.WorkspaceId ?? "");
    }

    /// <summary>
    /// Parses and saves manually edited global memory markdown and system instructions
    /// using optimistic revision control.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The target chat session.</param>
    /// <param name="revision">The revision read by the editor.</param>
    /// <param name="markdown">The edited global memory markdown.</param>
    /// <param name="systemMessage">The replacement system message.</param>
    /// <returns>The updated memory view.</returns>
    public async Task<AdminChatMemoryView> SaveAsync(int userId, int sessionId, string revision,
        string markdown, string systemMessage)
    {
        var session = await AuthorizeAsync(userId, sessionId);
        ValidateSystemMessage(systemMessage);
        var entries = AdminChatMemoryDocument.Parse(markdown);
        return View(await _store.UpdateAsync(TenantId, userId, revision, profile =>
        {
            var oldGlobal = profile.Entries.Where(entry => entry.WorkspaceId == "").ToList();
            if (AdminChatMemoryDocument.Export(oldGlobal) != markdown)
            {
                var merged = entries.Select(entry =>
                {
                    var previous = oldGlobal.FirstOrDefault(old => old.Text == entry.Text);
                    if (previous != null && previous.Tier == entry.Tier && previous.Topic == entry.Topic && previous.Pinned)
                        return previous;
                    return previous == null ? entry : previous with
                    {
                        Tier = entry.Tier, Topic = entry.Topic, Pinned = true,
                        Version = previous.Version + 1, UpdatedAt = DateTimeOffset.UtcNow, Source = "manual"
                    };
                }).ToList();
                if (merged.Count + profile.Entries.Count(entry => entry.WorkspaceId != "") > AdminChatMemoryDocument.MaxEntries)
                    throw new NcfExceptionBase("Memory allows at most 80 entries across all scopes.");
                foreach (var old in oldGlobal)
                {
                    var next = merged.FirstOrDefault(entry => entry.Id == old.Id);
                    if (next != old) profile.Record(old, next, "markdown");
                }
                foreach (var next in merged.Where(entry => oldGlobal.All(old => old.Id != entry.Id)))
                    profile.Record(null, next, "markdown");
                profile.Entries = profile.Entries.Where(entry => entry.WorkspaceId != "").Concat(merged).ToList();
            }
            profile.SystemMessage = systemMessage;
        }), sessionId, session?.WorkspaceId ?? "");
    }

    /// <summary>
    /// Maps the legacy enabled flag to Learn or Off mode and persists it.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="revision">The expected profile revision.</param>
    /// <param name="enabled">Whether learning should be enabled.</param>
    /// <returns>The updated memory view.</returns>
    public Task<AdminChatMemoryView> SetEnabledAsync(int userId, int sessionId, string revision, bool enabled) =>
        SetModeAsync(userId, sessionId, revision, enabled ? AdminChatMemoryMode.Learn : AdminChatMemoryMode.Off);

    /// <summary>
    /// Validates a requested memory mode and confirms PromptRange availability
    /// before enabling Learn mode.
    /// </summary>
    /// <param name="mode">The requested memory mode.</param>
    /// <exception cref="NcfExceptionBase">Thrown when the mode is invalid or its provider is unavailable.</exception>
    public async Task PrepareModeAsync(AdminChatMemoryMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new NcfExceptionBase("Invalid Memory mode.");
        if (mode == AdminChatMemoryMode.Learn)
        {
            var provider = _services.GetService<IMemoryPromptProvider>()
                ?? throw new NcfExceptionBase("Enable the PromptRange module before using Memory.");
            await provider.GetAsync(0);
        }
    }

    /// <summary>
    /// Changes the memory mode for an owner-scoped session and persists the mode map.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="revision">The expected profile revision.</param>
    /// <param name="mode">The desired memory mode.</param>
    /// <returns>The updated memory view.</returns>
    public async Task<AdminChatMemoryView> SetModeAsync(int userId, int sessionId, string revision, AdminChatMemoryMode mode)
    {
        if (sessionId <= 0) throw new NcfExceptionBase("Select a Chat session first.");
        var session = await AuthorizeAsync(userId, sessionId);
        await PrepareModeAsync(mode);
        return View(await _store.UpdateAsync(TenantId, userId, revision, profile =>
        {
            profile.Sessions.Remove(sessionId);
            if (mode == AdminChatMemoryMode.Off) profile.SessionModes.Remove(sessionId);
            else profile.SessionModes[sessionId] = mode;
        }), sessionId, session.WorkspaceId ?? "");
    }

    /// <summary>
    /// Adds, updates, or removes one validated memory entry in global or workspace scope.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="revision">The expected profile revision.</param>
    /// <param name="edit">The validated entry edit.</param>
    /// <returns>The updated memory view.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the edit is invalid, stale, or outside the workspace.</exception>
    public async Task<AdminChatMemoryView> EditAsync(int userId, int sessionId, string revision, AdminChatMemoryEdit edit)
    {
        var session = await AuthorizeAsync(userId, sessionId);
        var workspace = session?.WorkspaceId ?? "";
        if (edit == null || edit.Scope is not ("global" or "workspace")
            || edit.Scope == "workspace" && workspace.Length == 0
            || edit.Tier is not ("core" or "topic" or "archive")
            || edit.Kind is not ("preference" or "project" or "decision" or "reference")
            || string.IsNullOrWhiteSpace(edit.Text) || edit.Text.Length > 300 || edit.Text.Any(char.IsControl)
            || edit.Topic == null || edit.Topic.Length > 40 || edit.Topic.Any(char.IsControl)
            || edit.Tier == "topic" && edit.Topic.Length == 0)
            throw new NcfExceptionBase("Invalid Memory entry or unavailable Workspace.");
        return View(await _store.UpdateAsync(TenantId, userId, revision, profile =>
        {
            var old = profile.Entries.FirstOrDefault(entry => entry.Id == edit.Id);
            if (old == null && (!string.IsNullOrEmpty(edit.Id) || edit.Remove)
                || old != null && (old.Version != edit.Version || old.WorkspaceId != "" && old.WorkspaceId != workspace))
                throw new NcfExceptionBase("Memory entry changed or is outside this Workspace. Reload before editing.");
            var next = edit.Remove ? null : new AdminChatMemoryEntry(old?.Key ?? Guid.NewGuid().ToString("N"),
                edit.Text.Trim(), edit.Topic.Trim(), edit.Tier, edit.Pinned, DateTimeOffset.UtcNow)
            {
                Id = old?.Id ?? Guid.NewGuid().ToString("N"), Version = (old?.Version ?? 0) + 1,
                WorkspaceId = edit.Scope == "workspace" ? workspace : "", Kind = edit.Kind,
                Source = "manual", ExpiresAt = edit.ExpiresAt
            };
            if (old == null && profile.Entries.Count >= AdminChatMemoryDocument.MaxEntries)
                throw new NcfExceptionBase("Memory allows at most 80 entries.");
            if (old != null) profile.Entries.Remove(old);
            if (next != null) profile.Entries.Add(next);
            profile.Record(old, next, "manual");
        }), sessionId, workspace);
    }

    /// <summary>
    /// Reads the tenant-global administrator memory profile.
    /// </summary>
    /// <returns>The global profile.</returns>
    public Task<AdminChatMemoryProfile> GetGlobalAsync() => _store.ReadAsync(TenantId, 0);

    /// <summary>
    /// Saves the tenant-global system message with optimistic revision checking.
    /// </summary>
    /// <param name="revision">The expected global profile revision.</param>
    /// <param name="systemMessage">The replacement system message.</param>
    /// <returns>The updated global profile.</returns>
    public Task<AdminChatMemoryProfile> SaveGlobalAsync(string revision, string systemMessage)
    {
        ValidateSystemMessage(systemMessage);
        return _store.UpdateAsync(TenantId, 0, revision, profile => profile.SystemMessage = systemMessage);
    }

    /// <summary>
    /// Enforces the maximum system-message length.
    /// </summary>
    /// <param name="text">The system message to validate.</param>
    /// <exception cref="NcfExceptionBase">Thrown when the message exceeds 2000 characters.</exception>
    private static void ValidateSystemMessage(string text)
    {
        if (text == null || text.Length > 2000) throw new NcfExceptionBase("SystemMessage allows at most 2000 characters.");
    }

    private static AdminChatMemoryView View(AdminChatMemoryProfile profile, int sessionId, string workspace) =>
        new(profile.Revision, AdminChatMemoryDocument.Export(profile.Entries.Where(entry => entry.WorkspaceId == "")),
            profile.SystemMessage, profile.ModeFor(sessionId) != AdminChatMemoryMode.Off,
            profile.Entries.Count, AdminChatMemoryDocument.ContextByteBudget)
        {
            Mode = profile.ModeFor(sessionId), WorkspaceId = workspace,
            Entries = profile.Entries.Where(entry => entry.WorkspaceId == "" || entry.WorkspaceId == workspace).ToList(),
            History = profile.History.Where(item =>
                (item.Before == null || item.Before.WorkspaceId == "" || item.Before.WorkspaceId == workspace)
                && (item.After == null || item.After.WorkspaceId == "" || item.After.WorkspaceId == workspace)).Reverse().ToList()
        };

    /// <summary>
    /// Loads memory context for a chat turn, selects eligible retrieval entries,
    /// and captures recent user/assistant messages needed for Learn mode.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="userMessage">The current user message.</param>
    /// <param name="cancellationToken">Token used to cancel profile/message reads.</param>
    /// <param name="modelName">The model name used for token estimation.</param>
    /// <returns>The memory context and system instructions for the turn.</returns>
    public async Task<AdminChatMemoryContext> BeginAsync(int userId, int sessionId, string userMessage,
        CancellationToken cancellationToken, string modelName = "")
    {
        var session = await AuthorizeAsync(userId, sessionId);
        if (sessionId <= 0) throw new NcfExceptionBase("Memory context requires a Chat session.");
        var tenant = TenantId;
        var profile = await _store.ReadAsync(tenant, userId, cancellationToken);
        var global = await _store.ReadAsync(tenant, 0, cancellationToken);
        var mode = profile.ModeFor(sessionId);
        _updateRequested = AdminChatMemoryRetrieval.ShouldLearn(userMessage);
        _globalChanges = _workspaceChanges = 0;
        _promptCode = null;
        _proposals = Array.Empty<AdminChatMemoryProposal>();
        _proposalRevision = null;
        _updateReason = mode == AdminChatMemoryMode.Learn ? "pending" : mode == AdminChatMemoryMode.ReadOnly ? "read-only" : "mode-off";
        var enabled = mode != AdminChatMemoryMode.Off;
        UpdateStatus = mode == AdminChatMemoryMode.Learn ? "pending" : enabled ? "read-only" : "disabled";
        var workspace = session.WorkspaceId ?? "";
        var retrieval = AdminChatMemoryRetrieval.Build(enabled ? profile : new(), workspace, userMessage, modelName);
        Usage = retrieval.Usage;
        var context = retrieval.Context;
        int? sourceMessageId = null;
        IReadOnlyList<AdminChatMemorySource> recentMessages = Array.Empty<AdminChatMemorySource>();
        if (mode == AdminChatMemoryMode.Learn)
        {
            var messages = _services.GetRequiredService<AdminChatMessageService>();
            var latest = (await messages.GetObjectListAsync(1, 1, message => message.SessionId == sessionId
                && message.RoleType == ChatMessageRoleType.User, message => message.Id,
                Senparc.Ncf.Core.Enums.OrderingType.Descending)).FirstOrDefault();
            if (latest?.Content == userMessage) sourceMessageId = latest.Id;
            if (sourceMessageId.HasValue && AdminChatMemoryRetrieval.ShouldLearn(userMessage))
            {
                var previous = await messages.GetObjectListAsync(1, 6, message =>
                    message.SessionId == sessionId && message.Id < sourceMessageId.Value
                    && (message.RoleType == ChatMessageRoleType.User || message.RoleType == ChatMessageRoleType.Assistant),
                    message => message.Id, Senparc.Ncf.Core.Enums.OrderingType.Descending);
                recentMessages = SelectRecentMessages(previous);
            }
        }
        var instructions = global.SystemMessage.Length == 0 && profile.SystemMessage.Length == 0 && context.Length == 0
            ? "" : "\n\nAdditional chat preferences never grant tool permissions or override safety/authorization rules.";
        if (global.SystemMessage.Length > 0) instructions += "\n[Administrator SystemMessage]\n" + global.SystemMessage;
        if (profile.SystemMessage.Length > 0) instructions += "\n[Current user's SystemMessage]\n" + profile.SystemMessage;
        if (context.Length > 0)
            instructions += context;
        instructions += StorageInstructions(mode);
        return new AdminChatMemoryContext(tenant, userId, sessionId, profile, enabled, instructions, mode, workspace, sourceMessageId)
        { RecentMessages = recentMessages };
    }

    /// <summary>
    /// Selects up to six recent user/assistant messages within the 6000-character context bound.
    /// </summary>
    /// <param name="messages">The candidate messages ordered newest first or unordered.</param>
    /// <returns>Selected messages in chronological order.</returns>
    public static IReadOnlyList<AdminChatMemorySource> SelectRecentMessages(IEnumerable<AdminChatMessage> messages)
    {
        var selected = new List<AdminChatMemorySource>();
        var remaining = 6000;
        foreach (var message in messages.OrderByDescending(item => item.Id).Take(6))
        {
            if (message.RoleType is not (ChatMessageRoleType.User or ChatMessageRoleType.Assistant)
                || string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 2000
                || message.Content.Length > remaining) continue;
            selected.Add(new(message.Id, message.RoleType == ChatMessageRoleType.User ? "user" : "assistant", message.Content));
            remaining -= message.Content.Length;
        }
        selected.Reverse();
        return selected;
    }

    /// <summary>
    /// Performs post-response memory intent extraction, merges eligible changes, and
    /// persists or exposes review proposals according to the active mode.
    /// </summary>
    /// <param name="context">The context captured before the chat turn.</param>
    /// <param name="userMessage">The user message used for intent extraction.</param>
    /// <param name="modelId">The model used by the memory prompt provider.</param>
    /// <param name="generate">The callback that executes the extraction prompt.</param>
    /// <param name="cancellationToken">Token used to cancel extraction and persistence.</param>
    /// <returns>A task that completes after the memory update status is set.</returns>
    public async Task CompleteAsync(AdminChatMemoryContext context, string userMessage, int modelId,
        Func<string, string, CancellationToken, Task<string>> generate, CancellationToken cancellationToken)
    {
        _updateRequested = AdminChatMemoryRetrieval.ShouldLearn(userMessage);
        _globalChanges = _workspaceChanges = 0;
        _promptCode = null;
        _proposals = Array.Empty<AdminChatMemoryProposal>();
        _proposalRevision = null;
        if (context?.Enabled != true || context.Mode != AdminChatMemoryMode.Learn)
        {
            UpdateStatus = context?.Enabled == true ? "read-only" : "disabled";
            _updateReason = context?.Enabled == true ? "read-only" : "mode-off";
            return;
        }
        // Keyword matching only selects turns for semantic intent classification.
        if (!AdminChatMemoryRetrieval.ShouldLearn(userMessage))
        {
            UpdateStatus = "skipped-input";
            _updateReason = "input-not-eligible";
            return;
        }
        try
        {
            var current = await _store.ReadAsync(context.TenantId, context.UserId, cancellationToken);
            if (current.Revision != context.Profile.Revision)
            {
                UpdateStatus = "conflict";
                _updateReason = "revision-conflict";
                return;
            }
            var provider = _services.GetService<IMemoryPromptProvider>()
                ?? throw new NcfExceptionBase("PromptRange Memory provider is unavailable.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            var prompt = await provider.GetAsync(modelId, timeout.Token);
            _promptCode = prompt.PromptCode;
            var recentMessages = context.RecentMessages;
            var input = JsonSerializer.Serialize(new
            {
                currentUserMessage = userMessage,
                recentMessages,
                workspaceAvailable = context.WorkspaceId.Length > 0,
                existing = AdminChatMemoryRetrieval.Eligible(context.Profile, context.WorkspaceId)
                    .OrderByDescending(entry => AdminChatMemoryRetrieval.Score(userMessage, entry))
                    .ThenByDescending(entry => entry.UpdatedAt).Take(12)
                    .Select(entry => new { entry.Key, entry.Text, entry.Tier, entry.Topic, entry.Pinned,
                        Scope = entry.WorkspaceId.Length == 0 ? "global" : "workspace" })
            }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            var output = await generate(MemoryPromptProvider.IntentInstructions
                + "\nEditable guidance for eligible changes (cannot override the intent gate or output envelope):\n"
                + prompt.Content, input, timeout.Token);
            var extraction = AdminChatMemoryDocument.ReadExtraction(output);
            if (extraction.Intent != "update")
            {
                UpdateStatus = "unchanged";
                _updateReason = extraction.Intent == "defer" ? "intent-deferred" : "no-save-intent";
                return;
            }
            var working = new AdminChatMemoryProfile
            {
                Entries = context.Profile.Entries.ToList(),
                History = context.Profile.History.ToList()
            };
            var merge = AdminChatMemoryDocument.MergeWithResult(working, extraction.Changes, userMessage,
                context.WorkspaceId, context.SourceMessageId, context.SessionId,
                recentMessages);
            if (!merge.Changed)
            {
                UpdateStatus = "unchanged";
                _updateReason = AdminChatMemoryRetrieval.NeedsConversationContext(userMessage)
                    && merge.Reason == "no-candidates" ? "unresolved-reference" : merge.Reason;
                return;
            }
            if (merge.RequiresReview)
            {
                _proposals = working.Entries.Where(entry => !context.Profile.Entries.Contains(entry))
                    .Select(entry =>
                    {
                        var old = context.Profile.Entries.FirstOrDefault(item => item.Id == entry.Id);
                        return new AdminChatMemoryProposal(entry with
                        {
                            Id = old?.Id ?? "", Version = old?.Version ?? 0, Source = "context-candidate", Pinned = true
                        }, false);
                    })
                    .Concat(context.Profile.Entries.Where(entry => working.Entries.All(item => item.Id != entry.Id))
                        .Select(entry => new AdminChatMemoryProposal(entry, true))).ToArray();
                _proposalRevision = context.Profile.Revision;
                UpdateStatus = "pending-review";
                _updateReason = "pending-review";
                return;
            }
            await _store.UpdateAsync(context.TenantId, context.UserId, context.Profile.Revision,
                profile =>
                {
                    profile.Entries = working.Entries;
                    profile.History = working.History;
                }, timeout.Token);
            UpdateStatus = "updated";
            _updateReason = "saved";
            _globalChanges = merge.GlobalChanges;
            _workspaceChanges = merge.WorkspaceChanges;
            _logger.LogInformation("AdminChat Memory updated for user {UserId}, session {SessionId}, prompt {PromptCode}",
                context.UserId, context.SessionId, prompt.PromptCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            UpdateStatus = "timeout";
            _updateReason = "timeout";
            _logger.LogWarning("AdminChat Memory update timed out for session {SessionId}", context.SessionId);
        }
        catch (Exception error) when (error is NcfExceptionBase or JsonException or IOException
            or CryptographicException or InvalidOperationException or System.Net.Http.HttpRequestException
            or System.Data.Common.DbException or Microsoft.SemanticKernel.KernelException
            or System.ClientModel.ClientResultException or Azure.RequestFailedException or TimeoutException)
        {
            UpdateStatus = "failed";
            _updateReason = error is AdminChatMemoryValidationException validation ? validation.Reason
                : error is JsonException ? "invalid-json" : "update-failed";
            // Do not log model output or exception messages which might contain private memories.
            _logger.LogWarning("AdminChat Memory update failed for session {SessionId}: {ErrorType}",
                context.SessionId, error.GetType().Name);
        }
    }
}

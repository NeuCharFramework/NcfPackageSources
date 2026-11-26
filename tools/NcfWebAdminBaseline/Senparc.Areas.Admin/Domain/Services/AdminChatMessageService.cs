/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatMessageService.cs
    File description: Related implementation.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261105
    Change description: v0.12.26 Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261125
    Change description: v0.12.29 Improved AdminChat conversation loading and command parameter feedback.

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.PromptRange.Abstractions.References;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.CO2NET.Trace;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminChatMessageService : BaseClientService<AdminChatMessage>
    {
        public AdminChatMessageService(IAdminChatMessageRepository repository, IServiceProvider serviceProvider)
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public async Task<(List<AdminChatMessage> messages, int totalCount)> GetSessionMessagesAsync(int sessionId, int pageIndex = 0, int pageSize = 50)
        {
            if (pageIndex == 0)
            {
                var allMessages = await base.GetFullListAsync(
                    m => m.SessionId == sessionId,
                    "Sequence ASC");
                return (allMessages.ToList(), allMessages.Count());
            }
            else
            {
                var result = await base.GetObjectListAsync(
                    pageIndex,
                    pageSize,
                    m => m.SessionId == sessionId,
                    m => m.Sequence,
                    Ncf.Core.Enums.OrderingType.Ascending);
                return (result.ToList(), result.TotalCount);
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<int> GetNextSequenceAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);
            var maxSequence = messages.Any() ? messages.Max(m => m.Sequence) : 0;
            return maxSequence + 1;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <param name="roleType">roletype</param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public async Task<AdminChatMessage> AddMessageAsync(
            int sessionId,
            ChatMessageRoleType roleType,
            string content,
            string modelIdentifier = null,
            int? trajectoryId = null,
            int? trajectorySequence = null,
            IReadOnlyList<AdminChatAttachment> attachments = null,
            IReadOnlyList<PromptReference> promptReferences = null)
        {
            if (promptReferences?.Count > 0)
            {
                if (roleType != ChatMessageRoleType.User)
                    throw new ArgumentException("Only user messages may declare prompt references.");
                var references = _serviceProvider.GetService<IPromptReferenceService>()
                    ?? throw new InvalidOperationException("PromptRange is unavailable. Remove prompt references to send plain text.");
                await references.ValidateAsync(content, promptReferences);
            }
            var sequence = await GetNextSequenceAsync(sessionId);
            var message = new AdminChatMessage(
                sessionId,
                roleType,
                content,
                sequence,
                modelIdentifier,
                trajectoryId,
                trajectorySequence);
            message.SetAttachments(attachments);
            message.SetPromptReferences(promptReferences);
            await base.SaveObjectAsync(message);
            return message;
        }

        /// <summary>
        /// Associates a message with an optional trajectory and sequence position.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="trajectoryId">The trajectory identifier, or <see langword="null"/> to clear it.</param>
        /// <param name="trajectorySequence">The trajectory sequence, or <see langword="null"/> to clear it.</param>
        /// <returns><see langword="true"/> when the message was found and updated.</returns>
        public async Task<bool> SetTrajectoryAsync(int messageId, int? trajectoryId, int? trajectorySequence)
        {
            var message = await base.GetObjectAsync(m => m.Id == messageId);
            if (message == null)
            {
                return false;
            }

            message.SetTrajectory(trajectoryId, trajectorySequence);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        /// Scores a prompt reference with the next completed assistant response,
        /// after verifying user ownership and reference membership.
        /// </summary>
        /// <param name="userId">The administrator requesting the score.</param>
        /// <param name="messageId">The user message containing the reference.</param>
        /// <param name="promptItemId">The referenced prompt-item identifier.</param>
        /// <returns>The AI-generated prompt reference score.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when authorization, message, reference, reply, or PromptRange validation fails.</exception>
        public async Task<PromptReferenceInfo> ScorePromptReferenceWithAIAsync(int userId, int messageId, int promptItemId)
        {
            if (userId <= 0) throw new NcfExceptionBase("Sign in to score a prompt.");
            var message = await GetObjectAsync(item => item.Id == messageId)
                ?? throw new NcfExceptionBase("Message not found.");
            var session = await _serviceProvider.GetRequiredService<AdminChatSessionService>()
                .GetSessionByIdAsync(message.SessionId, userId);
            if (session == null || message.RoleType != ChatMessageRoleType.User ||
                !message.GetPromptReferences().Any(reference => reference.PromptItemId == promptItemId))
                throw new NcfExceptionBase("Message or prompt reference is not accessible.");
            var following = await GetObjectListAsync(1, 1,
                item => item.SessionId == message.SessionId && item.Sequence > message.Sequence,
                item => item.Sequence, Ncf.Core.Enums.OrderingType.Ascending);
            var result = following.FirstOrDefault();
            if (result?.RoleType != ChatMessageRoleType.Assistant || string.IsNullOrWhiteSpace(result.Content))
                throw new NcfExceptionBase("This prompt reference has no completed reply to score.");
            var provider = _serviceProvider.GetService<IPromptReferenceService>()
                ?? throw new NcfExceptionBase("PromptRange is unavailable.");
            return await provider.ScoreWithAIAsync(promptItemId, message.Content, result.Content);
        }

        /// <summary>
        /// Replaces overlapping prompt references on an owner-scoped user message,
        /// validates the resulting ranges, and persists the updated reference list.
        /// </summary>
        /// <param name="userId">The administrator requesting the update.</param>
        /// <param name="messageId">The user message identifier.</param>
        /// <param name="reference">The reference to add.</param>
        /// <returns>The updated message DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when access or PromptRange validation fails.</exception>
        public async Task<AdminChatMessageDto> AttachPromptReferenceAsync(
            int userId, int messageId, PromptReference reference)
        {
            if (userId <= 0) throw new NcfExceptionBase("Sign in to attach a prompt.");
            var message = await GetObjectAsync(item => item.Id == messageId)
                ?? throw new NcfExceptionBase("Message not found.");
            var session = await _serviceProvider.GetRequiredService<AdminChatSessionService>()
                .GetSessionByIdAsync(message.SessionId, userId);
            if (session == null) throw new NcfExceptionBase("Message is not accessible.");
            var provider = _serviceProvider.GetService<IPromptReferenceService>()
                ?? throw new NcfExceptionBase("PromptRange is unavailable.");

            var references = message.GetPromptReferences().ToList();
            references.RemoveAll(item =>
                item.Start < reference.Start + reference.Length &&
                reference.Start < item.Start + item.Length);
            references.Add(reference);
            references = references.OrderBy(item => item.Start).ToList();
            await provider.ValidateAsync(message.Content, references);
            message.SetPromptReferences(references);
            await base.SaveObjectAsync(message);
            return AdminChatMessageDto.CreateFromEntity(message);
        }

        /// <summary>
        /// Converts workspace-change and agent-task trajectory events into bounded
        /// attachment metadata for the completed chat response.
        /// </summary>
        /// <param name="sessionId">The chat session whose workspace was used.</param>
        /// <param name="harness">The harness result containing trajectory events.</param>
        /// <returns>Distinct task/action and workspace artifact attachments.</returns>
        public async Task<IReadOnlyList<AdminChatAttachment>> CollectWorkspaceArtifactsAsync(
            int sessionId, AdminChatHarnessResult harness)
        {
            if (harness?.TrajectoryEvents == null || harness.TrajectoryEvents.Count == 0)
                return Array.Empty<AdminChatAttachment>();
            var session = await _serviceProvider.GetRequiredService<AdminChatSessionService>()
                .GetObjectAsync(item => item.Id == sessionId);
            var storage = _serviceProvider.GetService<IWorkspaceStorage>();
            if (session == null || storage == null || string.IsNullOrWhiteSpace(session.WorkspaceId))
                return Array.Empty<AdminChatAttachment>();
            var changes = new Dictionary<string, string>(StringComparer.Ordinal);
            var actions = new List<AdminChatAttachment>();
            foreach (var trajectoryEvent in harness.TrajectoryEvents)
            {
                if (string.IsNullOrWhiteSpace(trajectoryEvent.PayloadJson))
                    continue;
                if (string.Equals(trajectoryEvent.EventType, "tool.result", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var actionDocument = JsonDocument.Parse(trajectoryEvent.PayloadJson);
                        CollectChatTaskActions(actionDocument.RootElement, actions);
                    }
                    catch (JsonException error)
                    {
                        SenparcTrace.SendCustomLog("AdminChat.ActionArtifact",
                            $"Ignored invalid action payload: {error.Message}");
                    }
                    continue;
                }
                if (!string.Equals(trajectoryEvent.EventType, "workspace.change", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    using var document = JsonDocument.Parse(trajectoryEvent.PayloadJson);
                    var change = document.RootElement.GetProperty("change");
                    var changeType = change.GetProperty("changeType").GetString();
                    var path = change.GetProperty("path").GetString();
                    if (string.IsNullOrWhiteSpace(path))
                        continue;
                    changes[path] = string.IsNullOrWhiteSpace(changeType) ? "changed" : changeType;
                    if (change.TryGetProperty("previousRelativePath", out var previousPathElement))
                    {
                        var previousPath = previousPathElement.GetString();
                        if (!string.IsNullOrWhiteSpace(previousPath)
                            && !string.Equals(previousPath, path, StringComparison.Ordinal))
                        {
                            changes[previousPath] = "deleted";
                        }
                    }
                }
                catch (Exception error) when (error is JsonException or InvalidOperationException)
                {
                    SenparcTrace.SendCustomLog("AdminChat.WorkspaceArtifact",
                        $"Ignored invalid trajectory payload: {error.Message}");
                }
            }
            var result = actions
                .GroupBy(action => action.ActionTargetId)
                .Select(group => group.First())
                .Take(20)
                .ToList();
            foreach (var (path, changeType) in changes.Take(80))
            {
                try
                {
                    var normalized = WorkspaceAssetPreviewBuilder.NormalizePath(path);
                    if (string.Equals(changeType, "deleted", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(new AdminChatAttachment
                        {
                            RelativePath = normalized,
                            Name = Path.GetFileName(normalized),
                            ContentType = "",
                            IsWorkspaceArtifact = true,
                            ChangeType = changeType,
                            PreviewKind = "file",
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                        continue;
                    }
                    var metadata = await storage.GetFileInfoAsync(session.WorkspaceId, normalized);
                    if (metadata.IsDirectory) continue;
                    result.Add(new AdminChatAttachment
                    {
                        RelativePath = normalized,
                        Name = Path.GetFileName(normalized),
                        ContentType = WorkspaceTextFiles.ResolveContentType(normalized),
                        Size = metadata.Length,
                        ETag = metadata.ETag,
                        IsWorkspaceArtifact = true,
                        ChangeType = changeType,
                        PreviewKind = WorkspaceTextFiles.Is3D(normalized) ? "3d"
                            : metadata.Length > 0 && WorkspaceTextFiles.ResolveContentType(normalized).StartsWith("image/")
                                ? "image"
                                : "file",
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
                catch (Exception error) when (error is ArgumentException or IOException
                    or FileNotFoundException or DirectoryNotFoundException)
                {
                    SenparcTrace.SendCustomLog("AdminChat.WorkspaceArtifact",
                        $"Artifact unavailable: {path}; {error.Message}");
                }
            }
            return result;
        }

        /// <summary>
        /// Recursively extracts agent-task action descriptors from arbitrary JSON payloads.
        /// </summary>
        /// <param name="element">The JSON element to inspect.</param>
        /// <param name="actions">The output action attachment list.</param>
        private static void CollectChatTaskActions(
            JsonElement element,
            List<AdminChatAttachment> actions)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectChatTaskActions(item, actions);
                    }
                    break;
                case JsonValueKind.String:
                    var text = element.GetString();
                    if (string.IsNullOrWhiteSpace(text)
                        || (!text.Contains("admin-chat-action", StringComparison.Ordinal)
                            && !text.Contains("agents-task", StringComparison.Ordinal)))
                    {
                        break;
                    }
                    try
                    {
                        using var nested = JsonDocument.Parse(text);
                        CollectChatTaskActions(nested.RootElement, actions);
                    }
                    catch (JsonException)
                    {
                        var start = text.IndexOf('{', StringComparison.Ordinal);
                        var end = text.LastIndexOf('}');
                        if (start >= 0 && end > start)
                        {
                            try
                            {
                                using var embedded = JsonDocument.Parse(text[start..(end + 1)]);
                                CollectChatTaskActions(embedded.RootElement, actions);
                            }
                            catch (JsonException)
                            {
                            }
                        }
                    }
                    break;
                case JsonValueKind.Object:
                    if (element.TryGetProperty("task", out var task)
                        && task.ValueKind == JsonValueKind.Object
                        && TryGetInt32(task, "ChatTaskId", out var taskId)
                        && taskId > 0)
                    {
                        var groupName = TryGetString(task, "ChatGroupName");
                        actions.Add(new AdminChatAttachment
                        {
                            Name = string.IsNullOrWhiteSpace(groupName)
                                ? $"ChatTask #{taskId}"
                                : groupName,
                            ActionType = "agents-task",
                            ActionTargetId = taskId,
                            PreviewKind = "action",
                            Notice = "Open the Agent task session",
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }
                    foreach (var property in element.EnumerateObject())
                    {
                        if (!string.Equals(property.Name, "task", StringComparison.OrdinalIgnoreCase))
                        {
                            CollectChatTaskActions(property.Value, actions);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Reads an integer property from an object using case-insensitive property matching.
        /// </summary>
        /// <param name="element">The JSON object to inspect.</param>
        /// <param name="name">The property name.</param>
        /// <param name="value">Receives the parsed integer.</param>
        /// <returns><see langword="true"/> when the property exists and is an integer.</returns>
        private static bool TryGetInt32(JsonElement element, string name, out int value)
        {
            value = 0;
            foreach (var property in element.EnumerateObject())
            {
                if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                return property.Value.TryGetInt32(out value);
            }
            return false;
        }

        /// <summary>
        /// Reads a string-like property from an object using case-insensitive matching.
        /// </summary>
        /// <param name="element">The JSON object to inspect.</param>
        /// <param name="name">The property name.</param>
        /// <returns>The string value, serialized value, or <see langword="null"/>.</returns>
        private static string TryGetString(JsonElement element, string name)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : property.Value.ToString();
                }
            }
            return null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Sets feedback on a message without applying an owner check.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="feedback">The feedback value.</param>
        /// <returns><see langword="true"/> when the message was found and updated.</returns>
        public async Task<bool> SetMessageFeedbackAsync(int messageId, MessageFeedbackType feedback)
        {
            var message = await base.GetObjectAsync(m => m.Id == messageId);
            if (message == null) return false;

            message.SetFeedback(feedback);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Replaces the persisted message content.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="newContent">The replacement content.</param>
        /// <returns><see langword="true"/> when the message was found and updated.</returns>
        public async Task<bool> UpdateMessageContentAsync(int messageId, string newContent)
        {
            var message = await base.GetObjectAsync(m => m.Id == messageId);
            if (message == null) return false;

            message.UpdateContent(newContent);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Deletes every message associated with a chat session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <returns>The number of messages deleted.</returns>
        public async Task<int> DeleteSessionMessagesAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);

            foreach (var message in messages)
            {
                await base.DeleteObjectAsync(message);
            }

            return messages.Count();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Deletes the distinct message identifiers that belong to a session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <param name="messageIds">The candidate message identifiers.</param>
        /// <returns>The number of existing messages deleted.</returns>
        public async Task<int> DeleteMessagesAsync(int sessionId, IEnumerable<int> messageIds)
        {
            var ids = (messageIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (!ids.Any())
            {
                return 0;
            }

            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId && ids.Contains(m.Id));
            var deletedCount = 0;

            foreach (var message in messages)
            {
                await base.DeleteObjectAsync(message);
                deletedCount++;
            }

            return deletedCount;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Returns the highest-sequence message in a session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <returns>The last message, or <see langword="null"/> when no messages exist.</returns>
        public async Task<AdminChatMessage> GetLastMessageAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId, "Sequence DESC");
            return messages.FirstOrDefault();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Counts messages belonging to a session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <returns>The number of persisted messages.</returns>
        public async Task<int> GetMessageCountAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return messages.Count();
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Loads a message only when its session belongs to the requested administrator.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <returns>The owned message, or <see langword="null"/> when inaccessible.</returns>
        public async Task<AdminChatMessage> GetOwnedMessageAsync(int messageId, int userId)
        {
            if (messageId <= 0 || userId <= 0)
            {
                return null;
            }

            return await base.GetObjectAsync(m => m.Id == messageId && m.Session.UserId == userId);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Sets feedback on a message after verifying that its session belongs to the user.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <param name="feedback">The feedback value.</param>
        /// <returns><see langword="true"/> when the owned message was updated.</returns>
        public async Task<bool> SetMessageFeedbackAsync(int messageId, int userId, MessageFeedbackType feedback)
        {
            var message = await GetOwnedMessageAsync(messageId, userId);
            if (message == null) return false;

            message.SetFeedback(feedback);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Searches messages across the administrator's active sessions using the
        /// shared history search and bounded result limit.
        /// </summary>
        /// <param name="userId">The administrator identifier.</param>
        /// <param name="keyword">The search keyword.</param>
        /// <param name="limit">The maximum result count.</param>
        /// <returns>Matching history hits.</returns>
        public async Task<IReadOnlyList<AdminChatHistorySearchHit>> SearchActiveHistoryAsync(
            int userId,
            string keyword,
            int limit = AdminChatHistorySearch.DefaultLimit)
        {
            if (userId <= 0 || AdminChatHistorySearch.NormalizeKeyword(keyword).Length == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var db = BaseData.BaseDB.BaseDataContext;
            var sessions = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .Where(session => session.UserId == userId && session.Status == ChatSessionStatus.Active)
                .ToListAsync();
            if (sessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var sessionIds = sessions.Select(session => session.Id).ToList();
            var messages = await db.Set<AdminChatMessage>()
                .AsNoTracking()
                .Where(message => sessionIds.Contains(message.SessionId))
                .ToListAsync();
            return AdminChatHistorySearch.Search(sessions, messages, userId, keyword, limit);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Searches tool-call trajectory events belonging to the administrator's
        /// active sessions and returns bounded history hits.
        /// </summary>
        /// <param name="userId">The administrator identifier.</param>
        /// <param name="keyword">The search keyword.</param>
        /// <param name="limit">The maximum result count.</param>
        /// <returns>Matching tool-call history hits.</returns>
        public async Task<IReadOnlyList<AdminChatHistorySearchHit>> SearchActiveToolCallsAsync(
            int userId,
            string keyword,
            int limit = AdminChatHistorySearch.DefaultLimit)
        {
            if (userId <= 0 || AdminChatHistorySearch.NormalizeKeyword(keyword).Length == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var db = BaseData.BaseDB.BaseDataContext;
            var sessions = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .Where(session => session.UserId == userId && session.Status == ChatSessionStatus.Active)
                .ToListAsync();
            if (sessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var sessionIds = sessions.Select(session => session.Id).ToList();
            var trajectories = await db.Set<AdminChatTrajectory>()
                .AsNoTracking()
                .Where(trajectory => trajectory.UserId == userId && sessionIds.Contains(trajectory.SessionId))
                .ToListAsync();
            if (trajectories.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var trajectoryIds = trajectories.Select(trajectory => trajectory.Id).ToList();
            var events = await db.Set<AdminChatTrajectoryEvent>()
                .AsNoTracking()
                .Where(item => trajectoryIds.Contains(item.TrajectoryId) && item.EventType == "tool.call")
                .ToListAsync();
            var messages = await db.Set<AdminChatMessage>()
                .AsNoTracking()
                .Where(message => sessionIds.Contains(message.SessionId) && message.TrajectoryId != null)
                .ToListAsync();
            return AdminChatHistorySearch.SearchToolCalls(sessions, trajectories, events, messages, userId, keyword, limit);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<List<(int UserId, int Count)>> GetMessageCountByUserAsync()
        {
            var db = base.BaseData.BaseDB.BaseDataContext;
            var perSession = await db.Set<AdminChatMessage>()
                .AsNoTracking()
                .GroupBy(m => m.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var sessions = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .Select(s => new { s.Id, s.UserId })
                .ToListAsync();

            var perSessionMap = perSession.ToDictionary(x => x.SessionId, x => x.Count);
            var result = new Dictionary<int, int>();
            foreach (var session in sessions)
            {
                if (perSessionMap.TryGetValue(session.Id, out var count))
                {
                    result[session.UserId] = (result.TryGetValue(session.UserId, out var existing) ? existing : 0) + count;
                }
            }
            return result.Select(kv => (kv.Key, kv.Value)).ToList();
        }
    }
}

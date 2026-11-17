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

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.PromptRange.Abstractions.References;
using Senparc.Ncf.Core.Exceptions;

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
        ///Legacy comment translated to English.
        /// </summary>
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
        public async Task<AdminChatMessage> GetLastMessageAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId, "Sequence DESC");
            return messages.FirstOrDefault();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<int> GetMessageCountAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return messages.Count();
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
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

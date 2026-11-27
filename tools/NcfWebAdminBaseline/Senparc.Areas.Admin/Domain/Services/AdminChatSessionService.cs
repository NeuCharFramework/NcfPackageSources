/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSessionService.cs
    File description: AdminChatSessionService.cs implementation and project behavior.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: validationUpdated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminChatSessionService : BaseClientService<AdminChatSession>
    {
        /// <summary>
        /// constructor
        /// </summary>
        public AdminChatSessionService(IAdminChatSessionRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="userId">userID</param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public async Task<(List<AdminChatSession> sessions, int totalCount)> GetUserActiveSessionsAsync(int userId, int pageIndex = 1, int pageSize = 20)
        {
            var result = await base.GetObjectListAsync(
                pageIndex, 
                pageSize, 
                s => s.UserId == userId && s.Status == ChatSessionStatus.Active, 
                s => s.LastMessageTime, 
                Ncf.Core.Enums.OrderingType.Descending);

            return (result.ToList(), result.TotalCount);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<(List<AdminChatSession> sessions, int totalCount)> GetUserArchivedSessionsAsync(int userId, int pageIndex = 1, int pageSize = 20)
        {
            var result = await base.GetObjectListAsync(
                pageIndex,
                pageSize,
                s => s.UserId == userId && s.Status == ChatSessionStatus.Archived,
                s => s.LastMessageTime,
                Ncf.Core.Enums.OrderingType.Descending);

            return (result.ToList(), result.TotalCount);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<AdminChatSession> GetSessionByIdAsync(int sessionId, int userId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId && s.UserId == userId);
            return session;
        }

        /// <summary>
        /// Moves one active session before or after another active session owned by
        /// the same user, tenant, and workspace, then rewrites the complete sibling
        /// ordering in one persistence operation.
        /// </summary>
        /// <param name="userId">The administrator who owns both sessions.</param>
        /// <param name="sessionId">The session being moved.</param>
        /// <param name="targetSessionId">The existing session used as the insertion anchor.</param>
        /// <param name="insertAfter">Whether the moved session is inserted after the anchor.</param>
        /// <returns>The reordered active sessions in their new order.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Thrown when identifiers are invalid or the sessions are not reorderable
        /// within the same user, tenant, and workspace boundary.
        /// </exception>
        public async Task<List<AdminChatSession>> ReorderSessionAsync(
            int userId, int sessionId, int targetSessionId, bool insertAfter)
        {
            if (userId <= 0 || sessionId <= 0 || targetSessionId <= 0 || sessionId == targetSessionId)
                throw new NcfExceptionBase("Invalid session order request.");

            var source = await GetSessionByIdAsync(sessionId, userId);
            var target = await GetSessionByIdAsync(targetSessionId, userId);
            if (source == null || target == null || source.Status != ChatSessionStatus.Active
                || target.Status != ChatSessionStatus.Active
                || source.TenantId != target.TenantId
                || (source.WorkspaceId ?? "") != (target.WorkspaceId ?? ""))
                throw new NcfExceptionBase("Only your active sessions in the same Workspace can be reordered.");

            var db = BaseData.BaseDB.BaseDataContext;
            var sessions = await db.Set<AdminChatSession>()
                .Where(s => s.TenantId == source.TenantId && s.UserId == userId && !s.Flag
                    && s.Status == ChatSessionStatus.Active && s.WorkspaceId == source.WorkspaceId)
                .OrderBy(s => s.SortOrder ?? int.MaxValue)
                .ThenByDescending(s => s.LastMessageTime)
                .ThenByDescending(s => s.Id)
                .ToListAsync();
            var moved = sessions.Single(s => s.Id == sessionId);
            sessions.Remove(moved);
            sessions.Insert(sessions.FindIndex(s => s.Id == targetSessionId) + (insertAfter ? 1 : 0), moved);
            for (var index = 0; index < sessions.Count; index++)
            {
                sessions[index].SetSortOrder(index);
                // Concurrent reorder requests must replace the whole order, not merge changed ranks.
                db.Entry(sessions[index]).Property(s => s.SortOrder).IsModified = true;
            }
            await db.SaveChangesAsync();
            return sessions;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Creates and persists a new administrator chat session with optional
        /// workspace association and workspace capability flags.
        /// </summary>
        /// <param name="title">The initial session title.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <param name="workspaceId">Optional workspace identifier.</param>
        /// <param name="workspaceWriteEnabled">Whether workspace writes are enabled.</param>
        /// <param name="workspaceCliEnabled">Whether workspace CLI operations are enabled.</param>
        /// <returns>The persisted session entity.</returns>
        public async Task<AdminChatSession> CreateSessionAsync(
            string title,
            int userId,
            string workspaceId = null,
            bool workspaceWriteEnabled = false,
            bool workspaceCliEnabled = false)
        {
            var session = new AdminChatSession(
                title,
                userId,
                workspaceId,
                workspaceWriteEnabled,
                workspaceCliEnabled);
            await base.SaveObjectAsync(session);
            return session;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Updates the title of an owner-scoped session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <param name="newTitle">The replacement title.</param>
        /// <returns><see langword="true"/> when the session was found and saved.</returns>
        public async Task<bool> UpdateSessionTitleAsync(int sessionId, int userId, string newTitle)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.UpdateTitle(newTitle);
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Updates the last-message timestamp for an existing session.
        /// Missing sessions are ignored.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A task that completes after the update is persisted.</returns>
        public async Task UpdateLastMessageTimeAsync(int sessionId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId);
            if (session != null)
            {
                session.UpdateLastMessageTime();
                await base.SaveObjectAsync(session);
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Archives an owner-scoped session unless it is missing or already deleted.
        /// Re-archiving an already archived session is idempotent.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <returns><see langword="true"/> when the requested state is achieved.</returns>
        public async Task<bool> ArchiveSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null || session.Status == ChatSessionStatus.Deleted) return false;
            if (session.Status == ChatSessionStatus.Archived) return true;

            session.Archive();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Sets the unread marker for an owner-scoped session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <param name="unread">The desired unread state.</param>
        /// <returns><see langword="true"/> when the state is applied or already matches.</returns>
        public async Task<bool> SetSessionUnreadAsync(int sessionId, int userId, bool unread)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null || session.Status == ChatSessionStatus.Deleted) return false;
            if (session.HasUnread == unread) return true;

            session.SetUnread(unread);
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Marks an owner-scoped session as deleted and persists the terminal state.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <returns><see langword="true"/> when the session was found and marked deleted.</returns>
        public async Task<bool> DeleteSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.Delete();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Restores an owner-scoped archived session to the active state.
        /// Missing, deleted, or already active sessions are handled idempotently.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <returns><see langword="true"/> when the session is active after the call.</returns>
        public async Task<bool> RestoreSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null || session.Status == ChatSessionStatus.Deleted) return false;
            if (session.Status == ChatSessionStatus.Active) return true;

            session.Restore();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Counts the sessions owned by an administrator that have the specified status.
        /// </summary>
        /// <param name="userId">The administrator identifier.</param>
        /// <param name="status">The session status to count.</param>
        /// <returns>The number of matching sessions.</returns>
        public async Task<int> GetSessionCountByStatusAsync(int userId, ChatSessionStatus status)
        {
            var sessions = await base.GetFullListAsync(s => s.UserId == userId && s.Status == status);
            return sessions.Count();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Aggregates session totals by administrator, including active, archived,
        /// deleted, and most-recent activity counts.
        /// </summary>
        /// <returns>One aggregate record per administrator represented in persistence.</returns>
        public async Task<List<AdminChatUserSessionStat>> GetUserSessionStatsAsync()
        {
            var db = base.BaseData.BaseDB.BaseDataContext;
            var stats = await db.Set<AdminChatSession>()
                .AsNoTracking()
                .GroupBy(s => s.UserId)
                .Select(g => new AdminChatUserSessionStat
                {
                    UserId = g.Key,
                    TotalSessionCount = g.Count(),
                    ActiveSessionCount = g.Count(s => s.Status == ChatSessionStatus.Active),
                    ArchivedSessionCount = g.Count(s => s.Status == ChatSessionStatus.Archived),
                    DeletedSessionCount = g.Count(s => s.Status == ChatSessionStatus.Deleted),
                    LastActiveTime = g.Max(s => s.LastMessageTime)
                })
                .ToListAsync();
            return stats;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminChatUserSessionStat
    {
        public int UserId { get; set; }
        public int TotalSessionCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public int ArchivedSessionCount { get; set; }
        public int DeletedSessionCount { get; set; }
        public DateTime LastActiveTime { get; set; }
    }
}

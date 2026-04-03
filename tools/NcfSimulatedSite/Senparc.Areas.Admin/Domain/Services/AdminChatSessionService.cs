using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///AdminChatSessionService: Manage background chat session service
    /// </summary>
    public class AdminChatSessionService : BaseClientService<AdminChatSession>
    {
        /// <summary>
        ///Constructor
        /// </summary>
        public AdminChatSessionService(IAdminChatSessionRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// Get the user's active conversation list (in descending order of last message time)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageIndex">Page number (starting from 1)</param>
        /// <param name="pageSize">Number per page</param>
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
        /// Get the session based on ID (including verifying user permissions)
        /// </summary>
        public async Task<AdminChatSession> GetSessionByIdAsync(int sessionId, int userId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId && s.UserId == userId);
            return session;
        }

        /// <summary>
        ///Create new chat session
        /// </summary>
        public async Task<AdminChatSession> CreateSessionAsync(string title, int userId)
        {
            var session = new AdminChatSession(title, userId);
            await base.SaveObjectAsync(session);
            return session;
        }

        /// <summary>
        ///update session title
        /// </summary>
        public async Task<bool> UpdateSessionTitleAsync(int sessionId, int userId, string newTitle)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.UpdateTitle(newTitle);
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// Update the last message time of the session
        /// </summary>
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
        ///archive session
        /// </summary>
        public async Task<bool> ArchiveSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.Archive();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// Delete session (soft delete)
        /// </summary>
        public async Task<bool> DeleteSessionAsync(int sessionId, int userId)
        {
            var session = await GetSessionByIdAsync(sessionId, userId);
            if (session == null) return false;

            session.Delete();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        ///Restore deleted session
        /// </summary>
        public async Task<bool> RestoreSessionAsync(int sessionId, int userId)
        {
            var session = await base.GetObjectAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session == null) return false;

            session.Restore();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// Get the total number of sessions (by status)
        /// </summary>
        public async Task<int> GetSessionCountByStatusAsync(int userId, ChatSessionStatus status)
        {
            var sessions = await base.GetFullListAsync(s => s.UserId == userId && s.Status == status);
            return sessions.Count();
        }
    }
}

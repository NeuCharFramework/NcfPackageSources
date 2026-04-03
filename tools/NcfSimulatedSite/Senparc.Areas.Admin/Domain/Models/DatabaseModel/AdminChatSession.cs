using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    ///AdminChatSession: Manage background chat sessions
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSession))] //The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class AdminChatSession : EntityBase<int>
    {
        /// <summary>
        /// Conversation title (automatically extracted from the first message, up to 150 characters)
        /// </summary>
        [Required, MaxLength(150)]
        public string Title { get; private set; }

        /// <summary>
        ///UserID (foreign key to AdminUserInfo)
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// session state
        /// </summary>
        [Required]
        public ChatSessionStatus Status { get; private set; }

        /// <summary>
        ///Last message time
        /// </summary>
        public DateTime LastMessageTime { get; private set; }

        /// <summary>
        /// Private constructor (for use by EF Core)
        /// </summary>
        private AdminChatSession() { }

        /// <summary>
        ///Create new chat session
        /// </summary>
        /// <param name="title">Session title</param>
        /// <param name="userId">User ID</param>
        public AdminChatSession(string title, int userId)
        {
            Title = title?.Length > 150 ? title.Substring(0, 150) : title ?? "新对话";
            UserId = userId;
            Status = ChatSessionStatus.Active;
            LastMessageTime = DateTime.Now;
        }

        /// <summary>
        ///update session title
        /// </summary>
        public void UpdateTitle(string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Title = title.Length > 150 ? title.Substring(0, 150) : title;
                base.SetUpdateTime();
            }
        }

        /// <summary>
        /// Update last message time
        /// </summary>
        public void UpdateLastMessageTime()
        {
            LastMessageTime = DateTime.Now;
            base.SetUpdateTime();
        }

        /// <summary>
        ///archive session
        /// </summary>
        public void Archive()
        {
            Status = ChatSessionStatus.Archived;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Delete session (soft delete, modify status)
        /// </summary>
        public void Delete()
        {
            Status = ChatSessionStatus.Deleted;
            base.SetUpdateTime();
        }

        /// <summary>
        ///restore session
        /// </summary>
        public void Restore()
        {
            Status = ChatSessionStatus.Active;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    ///Chat session status
    /// </summary>
    public enum ChatSessionStatus
    {
        /// <summary>
        ///active
        /// </summary>
        Active = 0,
        /// <summary>
        ///archived
        /// </summary>
        Archived = 1,
        /// <summary>
        /// deleted
        /// </summary>
        Deleted = 2
    }
}

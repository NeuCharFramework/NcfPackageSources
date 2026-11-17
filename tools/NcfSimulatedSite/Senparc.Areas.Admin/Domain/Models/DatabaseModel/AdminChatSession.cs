/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatSession.cs
    File description: Related implementation.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSession))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class AdminChatSession : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required, MaxLength(150)]
        public string Title { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public ChatSessionStatus Status { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime LastMessageTime { get; private set; }

        [MaxLength(64)]
        public string WorkspaceId { get; private set; }

        public bool WorkspaceWriteEnabled { get; private set; }

        public bool WorkspaceCliEnabled { get; private set; }

        public int? SortOrder { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool HasUnread { get; private set; }

        public void SetSortOrder(int sortOrder)
        {
            if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder));
            SortOrder = sortOrder;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private AdminChatSession() { }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <param name="userId">userID</param>
        public AdminChatSession(
            string title,
            int userId,
            string workspaceId = null,
            bool workspaceWriteEnabled = false,
            bool workspaceCliEnabled = false)
        {
            Title = title?.Length > 150 ? title.Substring(0, 150) : title ?? "新对话";
            UserId = userId;
            Status = ChatSessionStatus.Active;
            LastMessageTime = DateTime.Now;
            WorkspaceId = string.IsNullOrWhiteSpace(workspaceId) ? null : workspaceId.Trim();
            WorkspaceWriteEnabled = WorkspaceId != null && workspaceWriteEnabled;
            WorkspaceCliEnabled = WorkspaceId != null && workspaceCliEnabled;
        }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public void UpdateLastMessageTime()
        {
            LastMessageTime = DateTime.Now;
            base.SetUpdateTime();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void SetUnread(bool unread)
        {
            if (HasUnread == unread) return;
            HasUnread = unread;
            base.SetUpdateTime();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void Archive()
        {
            Status = ChatSessionStatus.Archived;
            HasUnread = false;
            base.SetUpdateTime();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void Delete()
        {
            Status = ChatSessionStatus.Deleted;
            HasUnread = false;
            base.SetUpdateTime();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void Restore()
        {
            Status = ChatSessionStatus.Active;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum ChatSessionStatus
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Active = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Archived = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Deleted = 2
    }
}

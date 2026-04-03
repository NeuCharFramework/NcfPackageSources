using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    ///AdminChatMessage: Manage background chat messages
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatMessage))]
    [Serializable]
    public class AdminChatMessage : EntityBase<int>
    {
        /// <summary>
        /// Owning session ID (foreign key to AdminChatSession)
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// Message role type (User/Assistant/System)
        /// </summary>
        [Required]
        public ChatMessageRoleType RoleType { get; private set; }

        /// <summary>
        /// Message content (supports Markdown format)
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// Message sequence number (used to maintain conversation order)
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        /// User feedback (Like/Dislike/None)
        /// </summary>
        public MessageFeedbackType UserFeedback { get; private set; }

        /// <summary>
        /// The model identifier to use (eg: "gpt-4", "claude-3")
        /// </summary>
        [MaxLength(100)]
        public string ModelIdentifier { get; private set; }

        /// <summary>
        /// Navigation properties: associated sessions
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public virtual AdminChatSession Session { get; private set; }

        /// <summary>
        /// Private constructor (for use by EF Core)
        /// </summary>
        private AdminChatMessage() { }

        /// <summary>
        ///Create new chat message
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="roleType">Role type</param>
        /// <param name="content">Message content</param>
        /// <param name="sequence">Message sequence number</param>
        /// <param name="modelIdentifier">Model identifier (optional)</param>
        public AdminChatMessage(int sessionId, ChatMessageRoleType roleType, string content, int sequence, string modelIdentifier = null)
        {
            SessionId = sessionId;
            RoleType = roleType;
            Content = content ?? string.Empty;
            Sequence = sequence;
            UserFeedback = MessageFeedbackType.None;
            ModelIdentifier = modelIdentifier;
        }

        /// <summary>
        ///Set user feedback
        /// </summary>
        public void SetFeedback(MessageFeedbackType feedback)
        {
            UserFeedback = feedback;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Update message content (usually used for appending in streaming output scenarios)
        /// </summary>
        public void UpdateContent(string newContent)
        {
            if (newContent != null)
            {
                Content = newContent;
                base.SetUpdateTime();
            }
        }
    }

    /// <summary>
    ///Chat message role type
    /// </summary>
    public enum ChatMessageRoleType
    {
        /// <summary>
        ///user messages
        /// </summary>
        User = 0,
        /// <summary>
        /// AI Assistant Message
        /// </summary>
        Assistant = 1,
        /// <summary>
        /// system message
        /// </summary>
        System = 2
    }

    /// <summary>
    ///Message feedback type
    /// </summary>
    public enum MessageFeedbackType
    {
        /// <summary>
        /// no feedback
        /// </summary>
        None = 0,
        /// <summary>
        /// Like
        /// </summary>
        Like = 1,
        /// <summary>
        /// click to dislike
        /// </summary>
        Dislike = 2
    }
}

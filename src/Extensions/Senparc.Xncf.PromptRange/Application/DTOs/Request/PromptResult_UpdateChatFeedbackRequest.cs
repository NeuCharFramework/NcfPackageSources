using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    /// Update conversation feedback request
    /// </summary>
    public class PromptResult_UpdateChatFeedbackRequest
    {
        /// <summary>
        ///Conversation record ID
        /// </summary>
        [Required]
        public int ChatId { get; set; }

        /// <summary>
        /// Feedback: Like (true), Unlike (false), Cancel feedback (null)
        /// </summary>
        public bool? Feedback { get; set; }
    }
}

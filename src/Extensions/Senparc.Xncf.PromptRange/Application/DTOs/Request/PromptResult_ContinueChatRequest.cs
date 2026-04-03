using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    /// continue chat request
    /// </summary>
    public class PromptResult_ContinueChatRequest
    {
        /// <summary>
        ///PromptResult ID
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        ///user messages
        /// </summary>
        [Required]
        public string UserMessage { get; set; }
    }
}

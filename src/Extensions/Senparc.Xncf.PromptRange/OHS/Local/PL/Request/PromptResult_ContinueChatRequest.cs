using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    /// 继续聊天请求
    /// </summary>
    public class PromptResult_ContinueChatRequest
    {
        /// <summary>
        /// PromptResult 的 ID
        /// </summary>
        [Required]
        public int PromptResultId { get; set; }

        /// <summary>
        /// 用户消息
        /// </summary>
        [Required]
        public string UserMessage { get; set; }
    }
}

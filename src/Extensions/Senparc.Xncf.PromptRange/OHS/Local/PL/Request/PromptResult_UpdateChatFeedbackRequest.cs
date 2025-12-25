using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    /// 更新对话反馈请求
    /// </summary>
    public class PromptResult_UpdateChatFeedbackRequest
    {
        /// <summary>
        /// 对话记录 ID
        /// </summary>
        [Required]
        public int ChatId { get; set; }

        /// <summary>
        /// 反馈：Like（true）、Unlike（false）、取消反馈（null）
        /// </summary>
        public bool? Feedback { get; set; }
    }
}






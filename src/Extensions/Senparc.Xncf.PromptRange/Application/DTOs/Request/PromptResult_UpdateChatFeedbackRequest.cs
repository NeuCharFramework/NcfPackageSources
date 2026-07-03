/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_UpdateChatFeedbackRequest.cs
    文件功能描述：PromptResult_UpdateChatFeedbackRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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

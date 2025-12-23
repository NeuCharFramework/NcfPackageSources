using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    /// <summary>
    /// 对话历史和 Prompt 内容响应
    /// </summary>
    public class PromptResult_ChatHistoryWithPromptResponse
    {
        /// <summary>
        /// 对话历史记录
        /// </summary>
        public List<PromptResultChatDto> ChatHistory { get; set; }
        
        /// <summary>
        /// Prompt 内容（SystemMessage）
        /// </summary>
        public string PromptContent { get; set; }
    }
}




/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_ChatHistoryWithPromptResponse.cs
    文件功能描述：PromptResult_ChatHistoryWithPromptResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
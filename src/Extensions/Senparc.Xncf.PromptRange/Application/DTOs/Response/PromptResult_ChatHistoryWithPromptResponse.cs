using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    /// <summary>
    ///Conversation history and prompt content response
    /// </summary>
    public class PromptResult_ChatHistoryWithPromptResponse
    {
        /// <summary>
        ///conversation history
        /// </summary>
        public List<PromptResultChatDto> ChatHistory { get; set; }
        
        /// <summary>
        ///Prompt content(SystemMessage)
        /// </summary>
        public string PromptContent { get; set; }
    }
}
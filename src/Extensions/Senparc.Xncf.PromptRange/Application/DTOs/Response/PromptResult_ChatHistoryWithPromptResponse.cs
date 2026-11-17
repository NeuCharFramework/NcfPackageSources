/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_ChatHistoryWithPromptResponse.cs
    File description: PromptResult_ChatHistoryWithPromptResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class PromptResult_ChatHistoryWithPromptResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<PromptResultChatDto> ChatHistory { get; set; }
        
        /// <summary>
        /// Prompt content（SystemMessage）
        /// </summary>
        public string PromptContent { get; set; }
    }
}
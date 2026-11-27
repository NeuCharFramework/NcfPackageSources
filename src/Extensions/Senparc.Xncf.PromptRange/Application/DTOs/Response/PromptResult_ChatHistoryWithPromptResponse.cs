/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_ChatHistoryWithPromptResponse.cs
    File description: PromptResult_ChatHistoryWithPromptResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    /// <summary>Contains prompt content and the ordered chat history for a result.</summary>
    public class PromptResult_ChatHistoryWithPromptResponse
    {
        /// <summary>Gets or sets the ordered chat history.</summary>
        public List<PromptResultChatDto> ChatHistory { get; set; }
        
        /// <summary>
        /// Prompt content（SystemMessage）
        /// </summary>
        public string PromptContent { get; set; }
    }
}

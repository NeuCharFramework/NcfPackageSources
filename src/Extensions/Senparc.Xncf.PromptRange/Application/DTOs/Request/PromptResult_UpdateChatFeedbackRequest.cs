/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptResult_UpdateChatFeedbackRequest.cs
    File description: PromptResult_UpdateChatFeedbackRequest related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class PromptResult_UpdateChatFeedbackRequest
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int ChatId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool? Feedback { get; set; }
    }
}

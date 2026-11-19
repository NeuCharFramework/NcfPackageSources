/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_HumanScoreRequest.cs
    File description: PromptResult_HumanScoreRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_HumanScoreRequest
    {
        // public int PromptItemId { get; set; }

        public int PromptResultId { get; set; }
        public decimal HumanScore { get; set; }
    }
}
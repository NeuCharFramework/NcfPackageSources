/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_RobotScoreRequest.cs
    File description: PromptResult_RobotScoreRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_RobotScoreRequest
    {
        public List<int> PromptResultId { get; set; }

        public List<string> ExpectedResultList { get; set; }
    }
}
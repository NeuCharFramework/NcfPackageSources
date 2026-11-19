/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_HistoryScoreResponse.cs
    File description: PromptItem_HistoryScoreResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptItem_HistoryScoreResponse
    {
        public List<string> XList { get; set; }

        public List<decimal> YList { get; set; }

        public List<decimal> ZList { get; set; }

        public PromptItem_HistoryScoreResponse(List<decimal> zList)
        {
            ZList = zList;
        }

        public PromptItem_HistoryScoreResponse(List<string> xList, List<decimal> yList, List<decimal> zList)
        {
            XList = xList;
            YList = yList;
            ZList = zList;
        }
    }
}
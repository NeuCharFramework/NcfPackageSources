/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Statistics_TodayTacticResponse.cs
    File description: Statistics_TodayTacticResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    [Serializable]
    public class Statistics_TodayTacticResponse
    {
        public DateTime QueryTime { get; set; } = DateTime.Now;

        public int TotalCount { get; set; }

        /// <summary>Initializes a daily tactic-count response.</summary>
        /// <param name="totalCount">The count included in the response.</param>
        public Statistics_TodayTacticResponse(int totalCount)
        {
            TotalCount = totalCount;
        }
    }
}

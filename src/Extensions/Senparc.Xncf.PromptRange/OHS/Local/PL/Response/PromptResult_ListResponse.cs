using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptResult_ListResponse
    {
        public int PromptItemId { get; set; }
        public List<PromptResult> PromptResults { get; set; }
        public DateTime QueryTime { get; set; } = DateTime.Now;

        public PromptResult_ListResponse(int promptItemId, List<PromptResult> promptResults)
        {
            PromptItemId = promptItemId;
            PromptResults = promptResults;
        }

        public PromptResult_ListResponse()
        {
        }
    }
}
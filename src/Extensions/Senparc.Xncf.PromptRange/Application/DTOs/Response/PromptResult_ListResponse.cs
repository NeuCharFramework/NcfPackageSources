using System;
using System.Collections.Generic;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptResult_ListResponse
    {
        public int PromptItemId { get; set; }

        public PromptItemDto PromptItem { get; set; }
        public List<PromptResultDto> PromptResults { get; set; }
        public DateTime QueryTime { get; set; } = DateTime.Now;

        public PromptResult_ListResponse(int promptItemId, PromptItemDto promptItem, List<PromptResultDto> promptResults)
        {
            PromptItemId = promptItemId;
            PromptItem = promptItem;
            PromptResults = promptResults;
        }

        public PromptResult_ListResponse()
        {
        }
    }
}
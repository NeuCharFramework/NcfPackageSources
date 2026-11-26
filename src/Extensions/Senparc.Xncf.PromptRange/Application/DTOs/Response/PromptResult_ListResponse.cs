/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResult_ListResponse.cs
    File description: PromptResult_ListResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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

        /// <summary>Initializes a prompt-result list response.</summary>
        /// <param name="promptItemId">The prompt item identifier.</param>
        /// <param name="promptItem">The prompt item DTO.</param>
        /// <param name="promptResults">The prompt result records.</param>
        public PromptResult_ListResponse(int promptItemId, PromptItemDto promptItem, List<PromptResultDto> promptResults)
        {
            PromptItemId = promptItemId;
            PromptItem = promptItem;
            PromptResults = promptResults;
        }

        /// <summary>Initializes an empty response for model binding.</summary>
        public PromptResult_ListResponse()
        {
        }
    }
}

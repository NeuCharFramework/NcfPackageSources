/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_ListResponse.cs
    文件功能描述：PromptResult_ListResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
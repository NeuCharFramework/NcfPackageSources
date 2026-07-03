/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Statistics_TodayTacticResponse.cs
    文件功能描述：Statistics_TodayTacticResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    [Serializable]
    public class Statistics_TodayTacticResponse
    {
        public DateTime QueryTime { get; set; } = DateTime.Now;

        public int TotalCount { get; set; }

        public Statistics_TodayTacticResponse(int totalCount)
        {
            TotalCount = totalCount;
        }
    }
}
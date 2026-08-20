/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItem_HistoryScoreResponse.cs
    文件功能描述：PromptItem_HistoryScoreResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
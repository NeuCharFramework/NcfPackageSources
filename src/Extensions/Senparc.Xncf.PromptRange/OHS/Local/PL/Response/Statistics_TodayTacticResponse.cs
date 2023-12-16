using System;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
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
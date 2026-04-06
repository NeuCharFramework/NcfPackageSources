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
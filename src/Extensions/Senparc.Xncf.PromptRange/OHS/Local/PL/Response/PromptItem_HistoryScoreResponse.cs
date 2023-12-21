using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptItem_HistoryScoreResponse
    {
        public List<string> XList { get; set; }

        public List<int> YList { get; set; }
        
        public List<int> ZList { get; set; }

        public PromptItem_HistoryScoreResponse(List<int> zList)
        {
            ZList = zList;
        }

        public PromptItem_HistoryScoreResponse(List<string> xList, List<int> yList, List<int> zList)
        {
            XList = xList;
            YList = yList;
            ZList = zList;
        }
    }
}
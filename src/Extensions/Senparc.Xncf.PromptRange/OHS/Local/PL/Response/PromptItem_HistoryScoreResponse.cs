using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class PromptItem_HistoryScoreResponse
    {
        public List<string> XList { get; set; }

        public List<int> YList { get; set; }

        public PromptItem_HistoryScoreResponse()
        {
        }

        public PromptItem_HistoryScoreResponse(List<string> xList, List<int> yList)
        {
            XList = xList;
            YList = yList;
        }
    }
}
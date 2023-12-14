using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_RobotScoreRequest
    {
        public List<int> PromptResultId { get; set; }

        public List<string> ExpectedResultList { get; set; }
        
        
    }
}
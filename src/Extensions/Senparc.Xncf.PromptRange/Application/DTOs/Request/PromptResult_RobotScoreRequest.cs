/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_RobotScoreRequest.cs
    文件功能描述：PromptResult_RobotScoreRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_RobotScoreRequest
    {
        public List<int> PromptResultId { get; set; }

        public List<string> ExpectedResultList { get; set; }
    }
}
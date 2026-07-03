/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResult_HumanScoreRequest.cs
    文件功能描述：PromptResult_HumanScoreRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptResult_HumanScoreRequest
    {
        // public int PromptItemId { get; set; }

        public int PromptResultId { get; set; }
        public decimal HumanScore { get; set; }
    }
}
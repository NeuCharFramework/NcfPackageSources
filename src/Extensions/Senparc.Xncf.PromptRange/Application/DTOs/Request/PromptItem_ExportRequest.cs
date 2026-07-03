/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItem_ExportRequest.cs
    文件功能描述：PromptItem_ExportRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public class PromptItem_ExportRequest
{
    public List<int> RangeIds { get; set; }
    public List<int> Ids { get; set; } = null;
}
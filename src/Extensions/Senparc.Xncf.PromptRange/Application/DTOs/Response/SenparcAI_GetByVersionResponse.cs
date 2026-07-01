/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenparcAI_GetByVersionResponse.cs
    文件功能描述：SenparcAI_GetByVersionResponse 数据传输对象定义
    
    
    创建标识：Senparc - 20240105
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Senparc.AI.AgentKernel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

public class SenparcAI_GetByVersionResponse : BaseResponse
{
    public SenparcAiSetting SenparcAiSetting { get; set; }
    public PromptItemDto PromptItem { get; set; }

    public SenparcAI_GetByVersionResponse(SenparcAiSetting senparcAiSetting, PromptItemDto dto)
    {
        SenparcAiSetting = senparcAiSetting;
        PromptItem = dto;
    }
}
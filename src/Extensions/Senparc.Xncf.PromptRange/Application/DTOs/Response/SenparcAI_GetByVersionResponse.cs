/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcAI_GetByVersionResponse.cs
    File description: Related implementation.

    Created by: Senparc - 20240105

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

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
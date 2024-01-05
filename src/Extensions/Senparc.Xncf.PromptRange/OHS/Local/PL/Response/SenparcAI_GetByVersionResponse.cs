using Senparc.AI.Kernel;
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
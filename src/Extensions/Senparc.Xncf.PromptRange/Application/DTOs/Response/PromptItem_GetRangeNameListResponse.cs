/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_GetRangeNameListResponse.cs
    File description: PromptItem_GetRangeNameListResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetRangeNameListResponse : AppResponseBase<string>
    {
        public int Id { get; set; }

        public string RangeName { get; set; }
    }
}
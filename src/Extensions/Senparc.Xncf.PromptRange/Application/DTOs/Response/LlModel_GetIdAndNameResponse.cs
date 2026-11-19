/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModel_GetIdAndNameResponse.cs
    File description: LlModel_GetIdAndNameResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class LlModel_GetIdAndNameResponse : BaseResponse
    {
        // public int Id { get; set; }

        public string Alias { get; set; }
    }
}
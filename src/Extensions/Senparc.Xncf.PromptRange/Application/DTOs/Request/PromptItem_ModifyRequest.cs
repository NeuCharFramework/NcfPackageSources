/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_ModifyRequest.cs
    File description: PromptItem_ModifyRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class PromptItem_ModifyRequest
    {
        public int Id { get; set; }

        public string NickName { get; set; }

        public string Note { get; set; }
    }
}
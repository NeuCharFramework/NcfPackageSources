/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_ExportRequest.cs
    File description: PromptItem_ExportRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public class PromptItem_ExportRequest
{
    public List<int> RangeIds { get; set; }
    public List<int> Ids { get; set; } = null;
}
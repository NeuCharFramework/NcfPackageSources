/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRange_ViewPromptCodeRequest.cs
    File description: PromptRange_ViewPromptCodeRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.17.0-preview5 integrate shared resource localization and improve feature text for the PromptRange module

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using System.ComponentModel;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

/// <summary>Filters the PromptRange prompt-code view by range name.</summary>
public class PromptRange_ViewPromptCodeRequest : FunctionAppRequestBase
{
    /// <summary>Gets or sets the optional range-name filter.</summary>
    [LocalizedDescription(typeof(PromptRangeResource), "Parameter.PromptRange.FilterRange")]
    public string FilterRangeName { get; set; }
}

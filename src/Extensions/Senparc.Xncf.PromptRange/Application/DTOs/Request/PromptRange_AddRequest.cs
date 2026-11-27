/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRange_AddRequest.cs
    File description: PromptRange_AddRequest related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public class PromptRange_AddRequest
{
    /// <summary>
    /// rangecode（usercustom）
    /// </summary>
    public string Alias { get; set; }

    /// <summary>Gets or sets the required display name of the PromptRange.</summary>
    [Required]
    public string RangeName { get; set; }

    /// <summary>Gets or sets optional serialized expected results for evaluation.</summary>
    public string ExpectedResultsJson { get; set; }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: PromptSelectorResponse.cs
    File description: PromptSelectorResponse related definitions and implementation


    Created by: Senparc - 20261102

    Modified by: Senparc - 20261103
    Change description: v0.20.8 add PromptRange selector service, UI components, and tests

    Modified by: Senparc - 20261105
    Change description: v0.20.14 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

public class PromptSelectorOption
{
    public int RangeId { get; set; }
    public string RangeName { get; set; }
    public string RangeAlias { get; set; }
    public string Tactic { get; set; }
}

public class PromptSelectorItem : PromptSelectorOption
{
    public int Id { get; set; }
    public string NickName { get; set; }
    public string FullVersion { get; set; }
    public bool IsDraft { get; set; }
    public decimal EvalAvgScore { get; set; }
    public decimal EvalMaxScore { get; set; }
    public decimal CurrentScore { get; set; }
    public string Content { get; set; }
}

public class PromptSelectorSearchResponse
{
    public List<PromptSelectorItem> Items { get; set; }
    public int TotalCount { get; set; }
}

public class PromptSelectorScoreItem : PromptSelectorOption
{
    public int Id { get; set; }
    public string FullVersion { get; set; }
    public decimal CurrentScore { get; set; }
}

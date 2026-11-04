/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PromptSelectorResponse.cs
    文件功能描述：PromptSelectorResponse 相关定义与实现


    创建标识：Senparc - 20261102

    修改标识：Senparc - 20261103
    修改描述：v0.20.8 add PromptRange selector service, UI components, and tests

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

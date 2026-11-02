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
    public string Content { get; set; }
}

public class PromptSelectorSearchResponse
{
    public List<PromptSelectorItem> Items { get; set; }
    public int TotalCount { get; set; }
}

using System.Collections.Generic;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public class PromptItem_ExportRequest
{
    public List<int> RangeIds { get; set; }
    public List<int> Ids { get; set; } = null;
}
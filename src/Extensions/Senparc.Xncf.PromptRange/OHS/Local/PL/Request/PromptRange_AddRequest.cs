using Json.Schema.Generation;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public class PromptRange_AddRequest
{
    /// <summary>
    /// 靶场代号（用户自定义）
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// 靶场名称（来自版号生成）
    /// </summary>
    [Required]
    public string RangeName { get; set; }

    /// <summary>
    /// 期望结果Json
    /// </summary>
    public string ExpectedResultsJson { get; set; }
}
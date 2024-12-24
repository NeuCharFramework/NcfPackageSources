using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

public class PromptRangeDto : DtoBase
{
    /// <summary>
    /// 主键 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 靶场代号（用户自定义）
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// 靶场名称（来自版号生成）
    /// </summary>
    public string RangeName { get; set; }

    // /// <summary>
    // /// 期望结果Json
    // /// </summary>
    // public string ExpectedResultsJson { get;  set; }
}
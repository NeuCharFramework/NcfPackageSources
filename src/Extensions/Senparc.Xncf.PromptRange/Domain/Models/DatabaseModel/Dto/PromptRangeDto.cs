/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRangeDto.cs
    文件功能描述：PromptRangeDto 相关实现
    
    
    创建标识：Senparc - 20240105
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
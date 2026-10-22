/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRangeDto.cs
    文件功能描述：PromptRangeDto 相关实现
    
    
    创建标识：Senparc - 20240105
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20261021
    修改描述：v0.20.0 输出 Prompt 或 Skill 类型

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

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

    public SkillKind Kind { get; set; }

    public int? SourceFileId { get; set; }

    public string SourceType { get; set; }

    public string SourceUrl { get; set; }

    // /// <summary>
    // /// 期望结果Json
    // /// </summary>
    // public string ExpectedResultsJson { get;  set; }
}
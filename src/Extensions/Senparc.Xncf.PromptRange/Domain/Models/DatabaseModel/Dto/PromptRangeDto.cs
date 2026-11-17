/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeDto.cs
    File description: PromptRangeDto related implementation

    Created by: Senparc - 20240105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261021
    Change description: v0.20.0 output Prompt or Skill type

    Modified by: Senparc - 20261103
    Change description: v0.20.8 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

public class PromptRangeDto : DtoBase
{
    /// <summary>
    /// primary key ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// rangecode（usercustom）
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public string RangeName { get; set; }

    public SkillKind Kind { get; set; }

    public int? SourceFileId { get; set; }

    public string SourceType { get; set; }

    public string SourceUrl { get; set; }

    // /// <summary>
    //Legacy comment translated to English.
    // /// </summary>
    // public string ExpectedResultsJson { get;  set; }
}
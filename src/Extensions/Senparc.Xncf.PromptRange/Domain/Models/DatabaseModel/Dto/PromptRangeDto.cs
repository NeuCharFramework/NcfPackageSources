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

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

/// <summary>Transfers PromptRange identity and source metadata to application clients.</summary>
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

    /// <summary>Gets or sets the display name of the range.</summary>
    public string RangeName { get; set; }

    /// <summary>Gets or sets the prompt or skill kind.</summary>
    public SkillKind Kind { get; set; }

    /// <summary>Gets or sets the optional managed source file identifier.</summary>
    public int? SourceFileId { get; set; }

    /// <summary>Gets or sets the source type.</summary>
    public string SourceType { get; set; }

    /// <summary>Gets or sets the source URL.</summary>
    public string SourceUrl { get; set; }

    // /// <summary>
    //Legacy comment translated to English.
    // /// </summary>
    // public string ExpectedResultsJson { get;  set; }
}

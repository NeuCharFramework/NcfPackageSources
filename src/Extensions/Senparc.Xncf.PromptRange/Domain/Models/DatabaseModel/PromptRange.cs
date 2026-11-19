/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRange.cs
    File description: PromptRange related implementation

    Created by: Senparc - 20240105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

/// <summary>
/// PromptRange range
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(PromptRange))] /*a prefix is required to prevent system-wide conflicts*/
[Serializable]
public class PromptRange : EntityBase<int>
{
    /// <summary>
    /// rangecode（usercustom）
    /// </summary>
    [MaxLength(50)]
    //[CanBeNull]
    public string Alias { get; private set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Required, MaxLength(20)]
    //[NotNull]
    public string RangeName { get; private set; }

    public SkillKind Kind { get; private set; } = SkillKind.Prompt;

    public int? SourceFileId { get; private set; }

    [MaxLength(20)]
    public string SourceType { get; private set; }

    [MaxLength(2048)]
    public string SourceUrl { get; private set; }

    // /// <summary>
    //Legacy comment translated to English.
    // /// </summary>
    // [MaxLength(200)]
    // [CanBeNull]
    // public string ExpectedResultsJson { get; private set; }

    #region CTOR

    public PromptRange(string rangeName, string alias)
    {
        RangeName = rangeName;
        Alias = alias;
        AddTime = SystemTime.Now.DateTime;
        LastUpdateTime = SystemTime.Now.DateTime;
    }

    // public PromptRange(string alias, string rangeName, string expectedResultsJson)
    // {
    //     Alias = alias;
    //     RangeName = rangeName;
    //     // ExpectedResultsJson = expectedResultsJson;
    // }

    #endregion

    #region Method

    public PromptRange Rename(string alias)
    {
        this.Alias = alias;
        return this;
    }

    // public PromptRange UpdateExpectedResultsJson(string expectedResultsJson)
    // {
    //     this.ExpectedResultsJson = expectedResultsJson;
    //
    //     return this;
    // }

    public PromptRange ChangeAlias(string alias)
    {
        this.Alias = alias;

        return this;
    }

    public PromptRange ChangeKind(SkillKind kind)
    {
        Kind = kind;
        return this;
    }

    public PromptRange UpdateSource(int? sourceFileId, string sourceType, string sourceUrl)
    {
        SourceFileId = sourceFileId;
        SourceType = sourceType;
        SourceUrl = sourceUrl;
        return this;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    /// <returns></returns>
    public string GetAvailableName()
    {
        if (!this.Alias.IsNullOrEmpty())
        {
            return this.Alias;
        }
        else
        {
            return this.RangeName;
        }
    }

    #endregion
}
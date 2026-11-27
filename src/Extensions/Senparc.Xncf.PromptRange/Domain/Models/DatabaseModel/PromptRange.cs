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

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

/// <summary>
/// Stores the identity, source metadata, and skill kind of a PromptRange.
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

    /// <summary>Gets the required display name of the range.</summary>
    [Required, MaxLength(20)]
    //[NotNull]
    public string RangeName { get; private set; }

    /// <summary>Gets the range kind, such as prompt or skill.</summary>
    public SkillKind Kind { get; private set; } = SkillKind.Prompt;

    /// <summary>Gets the optional managed source file identifier.</summary>
    public int? SourceFileId { get; private set; }

    /// <summary>Gets the source type associated with the range.</summary>
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

    /// <summary>
    /// Initializes a prompt range identity.
    /// </summary>
    /// <param name="rangeName">The display range name.</param>
    /// <param name="alias">The stable range alias.</param>
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

    /// <summary>
    /// Replaces the range alias.
    /// </summary>
    /// <param name="alias">The new alias.</param>
    /// <returns>This range entity.</returns>
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

    /// <summary>
    /// Changes the range alias.
    /// </summary>
    /// <param name="alias">The new alias.</param>
    /// <returns>This range entity.</returns>
    public PromptRange ChangeAlias(string alias)
    {
        this.Alias = alias;

        return this;
    }

    /// <summary>
    /// Changes whether the range is a prompt or skill range.
    /// </summary>
    /// <param name="kind">The new skill kind.</param>
    /// <returns>This range entity.</returns>
    public PromptRange ChangeKind(SkillKind kind)
    {
        Kind = kind;
        return this;
    }

    /// <summary>
    /// Updates the optional source-file and source-URL metadata.
    /// </summary>
    /// <param name="sourceFileId">The optional FileManager file identifier.</param>
    /// <param name="sourceType">The source type.</param>
    /// <param name="sourceUrl">The source URL.</param>
    /// <returns>This range entity.</returns>
    public PromptRange UpdateSource(int? sourceFileId, string sourceType, string sourceUrl)
    {
        SourceFileId = sourceFileId;
        SourceType = sourceType;
        SourceUrl = sourceUrl;
        return this;
    }

    /// <summary>
    /// Returns the alias when configured, otherwise the display range name.
    /// </summary>
    /// <returns>The user-facing range name.</returns>
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
    /// <summary>Gets the source URL associated with the range.</summary>

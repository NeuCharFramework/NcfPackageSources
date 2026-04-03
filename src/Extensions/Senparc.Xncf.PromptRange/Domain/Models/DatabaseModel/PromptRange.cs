using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

/// <summary>
///PromptRange shooting range
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(PromptRange))] /*The prefix must be added to prevent conflicts system-wide.*/
[Serializable]
public class PromptRange : EntityBase<int>
{
    /// <summary>
    /// Shooting range code (user-defined)
    /// </summary>
    [MaxLength(50)]
    //[CanBeNull]
    public string Alias { get; private set; }

    /// <summary>
    /// Shooting range name (from version number generation)
    /// </summary>
    [Required, MaxLength(20)]
    //[NotNull]
    public string RangeName { get; private set; }

    // /// <summary>
    // /// Expected result Json
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

    /// <summary>
    /// Return this property when <see cref="Alias"/> is present, otherwise use <see cref="RangeName"/>
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
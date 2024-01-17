using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange;

[Table(Register.DATABASE_PREFIX + nameof(PromptRange))] /*必须添加前缀，防止全系统中发生冲突*/
[Serializable]
public class PromptRange : EntityBase<int>
{
    /// <summary>
    /// 靶场代号（用户自定义）
    /// </summary>
    [MaxLength(50)]
    [CanBeNull]
    public string Alias { get; private set; }

    /// <summary>
    /// 靶场名称（来自版号生成）
    /// </summary>
    [Required, MaxLength(20)]
    [NotNull]
    public string RangeName { get; private set; }

    // /// <summary>
    // /// 期望结果Json
    // /// </summary>
    // [MaxLength(200)]
    // [CanBeNull]
    // public string ExpectedResultsJson { get; private set; }

    #region CTOR

    public PromptRange(string rangeName)
    {
        RangeName = rangeName;
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

    #endregion
}
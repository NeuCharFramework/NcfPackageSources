using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange;

/// <summary>
/// PromptItem
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(PromptItem))] /*必须添加前缀，防止全系统中发生冲突*/
[Serializable]
public class PromptItem : EntityBase<int>
{
    /// <summary>
    /// 靶场 ID
    /// </summary>
    public int RangeId { get; private set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [MaxLength(50)]
    public string NickName { get; private set; }

    /// <summary>
    /// Prompt内容
    /// </summary>
    public string Content { get; private set; }

    #region 模型参数

    [Required] public int ModelId { get; private set; }

    /// <summary>
    /// TopP
    /// </summary>
    [Required]
    public float TopP { get; private set; }

    /// <summary>
    /// 温度
    /// </summary>
    [Required]
    public float Temperature { get; private set; }

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    [Required]
    public int MaxToken { get; private set; }

    /// <summary>
    /// 频率惩罚
    /// </summary>
    [Required]
    public float FrequencyPenalty { get; private set; }

    [Required] public float PresencePenalty { get; private set; }

    /// <summary>
    /// 停止序列（JSON 数组）
    /// </summary>
    [CanBeNull]
    public string StopSequences { get; private set; }

    #endregion

    #region 打分

    /// <summary>
    /// 评估参数, 平均分
    /// </summary>
    [MaxLength(3)]
    public decimal EvalAvgScore { get; private set; } = -1;

    /// <summary>
    /// 评估参数
    /// </summary>
    [MaxLength(3)]
    public decimal EvalMaxScore { get; private set; } = -1;

    /// <summary>
    /// 期望结果Json
    /// </summary>
    public string ExpectedResultsJson { get; private set; }

    #endregion

    #region Full Version

    /// <summary>
    /// <para>版本号，格式为 Name-Tactic-Aiming</para> 
    /// <example>2023.12.14.1-T1.1-A123</example>
    /// <para>Name: <inheritdoc cref="RangeName"/></para>
    /// <para>Tactic: <inheritdoc cref="Tactic"/></para>
    /// <para>Aiming: <inheritdoc cref="Aiming"/></para>
    ///         为   Tx              这里的x为分支号，str,允许1.1.1。。。
    ///      Aiming   为   Ax              这里的x为打靶次数，int
    /// </summary>
    [Required, MaxLength(50)]
    public string FullVersion
    {
        get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
        private set { }
    }

    /// <summary>
    /// 靶场名 -> 应该采用 PromptRange 中的名字
    /// <para>格式为 yyyy.MM.dd.x ,这里的x为当天生成的序号，int</para>
    /// <example>示例一
    ///     <code>2023.12.14.1</code>
    /// </example>
    /// <example>示例二
    ///     <code>2023.12.14.123</code>
    /// </example>
    /// </summary>
    [MaxLength(20)]
    public string RangeName { get; private set; }

    /// <summary>
    /// 靶道名
    /// <para>格式为 x.x.x... ,这里的x为分支号</para>
    /// <para>在完整的版本号中，应该用-T连接Name</para>
    /// 
    /// </summary>
    public string Tactic { get; private set; }

    /// <summary>
    /// <para>为打靶次数，int</para>
    /// </summary>
    [MaxLength(5)]
    public int Aiming { get; private set; }

    /// <summary>
    /// 父Tactic, 可以是空串
    /// </summary>
    [Required]
    public string ParentTac { get; private set; }

    #endregion

    #region Prompt请求参数

    /// <summary>
    /// 前缀
    /// </summary>
    [MaxLength(10)]
    public string Prefix { get; private set; }

    /// <summary>
    /// 后缀
    /// </summary>
    [MaxLength(10)]
    public string Suffix { get; private set; }

    /// <summary>
    /// 请求参数键值对 JSON 
    /// </summary>

    public string VariableDictJson { get; private set; }

    #endregion

    /// <summary>
    /// Note（可选）
    /// </summary>
    [MaxLength(20)]
    public string Note { get; private set; }

    /// <summary>
    /// 最后一次运行时间
    /// </summary>
    public DateTime LastRunTime { get; private set; } = DateTime.Now;

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsShare { get; private set; } = false;

    public bool IsDraft { get; private set; }


    #region ctor 构造函数

    /// <summary>
    /// 导入时使用
    /// </summary>
    /// <param name="promptRange"></param>
    /// <param name="nickName"></param>
    /// <param name="tactic"></param>
    public PromptItem(PromptRangeDto promptRange, string nickName, int tactic)
    {
        RangeId = promptRange.Id;
        RangeName = promptRange.RangeName;
        Tactic = tactic.ToString();
        ParentTac = "";
        Aiming = 1;
        IsDraft = true;
        NickName = nickName;
    }

    public PromptItem(PromptItemDto dto) : this(dto.RangeId, dto.Content, dto.ModelId, dto.TopP, dto.Temperature, dto.MaxToken,
        dto.FrequencyPenalty, dto.PresencePenalty, dto.StopSequences, dto.RangeName, dto.Tactic, dto.Aiming, dto.ParentTac, dto.Note,
        dto.IsDraft, dto.ExpectedResultsJson,
        dto.Prefix, dto.Suffix, dto.VariableDictJson)
    {
        Id = dto.Id;
        NickName = dto.NickName;
        EvalAvgScore = dto.EvalAvgScore;
        EvalMaxScore = dto.EvalMaxScore;
        LastRunTime = dto.LastRunTime;
        IsShare = dto.IsShare;
    }

    public PromptItem(int rangeId, string content,
        int modelId, float topP, float temperature, int maxToken, float frequencyPenalty, float presencePenalty, string stopSequences,
        string rangeName, string tactic, int aiming, string parentTac,
        string note, bool isDraft, string expectedResultsJson,
        string prefix, string suffix, string variableDictJson)
    {
        RangeId = rangeId;
        Content = content;
        ModelId = modelId;
        TopP = topP;
        Temperature = temperature;
        MaxToken = maxToken;
        FrequencyPenalty = frequencyPenalty;
        PresencePenalty = presencePenalty;
        StopSequences = stopSequences;
        RangeName = rangeName;
        Tactic = tactic;
        Aiming = aiming;
        ParentTac = parentTac;
        Note = note;
        IsDraft = isDraft;
        ExpectedResultsJson = expectedResultsJson;
        Prefix = prefix;
        Suffix = suffix;
        VariableDictJson = variableDictJson;
        EvalAvgScore = -1;
        EvalMaxScore = -1;
    }

    public PromptItem(int rangeId, string rangeName, string tactic, int aiming, string parentTac, PromptItem_AddRequest request) :
        this(rangeId, request.Content, request.ModelId, request.TopP, request.Temperature, request.MaxToken, request.FrequencyPenalty,
            request.PresencePenalty, request.StopSequences, rangeName, tactic, aiming, parentTac, request.Note, request.IsDraft,
            request.ExpectedResultsJson,
            request.Prefix, request.Suffix, request.VariableDictJson)
    {
    }

    #endregion

    public PromptItem UpdateExpectedResultsJson(string expectedResultsJson)
    {
        this.ExpectedResultsJson = expectedResultsJson;

        return this;
    }

    public PromptItem ShareSwitch(bool show)
    {
        this.IsShare = show;

        return this;
    }

    public PromptItem ModifyNickName(string nickName)
    {
        this.NickName = nickName;

        return this;
    }

    public PromptItem UpdateEvalAvgScore(decimal score)
    {
        this.EvalAvgScore = score;

        return this;
    }

    public PromptItem UpdateEvalMaxScore(decimal score)
    {
        this.EvalMaxScore = score;

        return this;
    }

    public PromptItem ModifyNote(string note)
    {
        this.Note = note;

        return this;
    }

    public PromptItem DraftSwitch(bool isDraft)
    {
        this.IsDraft = isDraft;

        return this;
    }

    /// <summary>
    /// 更新使用的模型参数
    /// </summary>
    /// <param name="topP"></param>
    /// <param name="temperature"></param>
    /// <param name="maxToken"></param>
    /// <param name="frequencyPenalty"></param>
    /// <param name="presencePenalty"></param>
    /// <param name="stopSequences"></param>
    /// <returns></returns>
    public PromptItem UpdateModelParam(float topP, float temperature, int maxToken, float frequencyPenalty, float presencePenalty,
        string stopSequences)
    {
        TopP = topP;
        Temperature = temperature;
        MaxToken = maxToken;
        FrequencyPenalty = frequencyPenalty;
        PresencePenalty = presencePenalty;
        StopSequences = stopSequences;

        return this;
    }

    public PromptItem UpdateVariablesJson(string variablesJson)
    {
        VariableDictJson = variablesJson;

        return this;
    }

    public PromptItem UpdateContent(string skPrompt)
    {
        Content = skPrompt;

        return this;
    }
}
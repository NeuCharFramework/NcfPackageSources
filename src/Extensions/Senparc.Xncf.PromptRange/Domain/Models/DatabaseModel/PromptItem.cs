using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

/// <summary>
/// PromptItem：每个不同版本的 Prompt 配置
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

    [Required]
    public int ModelId { get; private set; }

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

    #region 静态方法

    /// <summary>
    /// 判断是否为 Version 格式
    /// </summary>
    /// <returns></returns>
    public static bool IsPromptVersion(string versionOrNickName)
    {
        /* 判断是否为 Prompt 版本（从左到右，必须包含 -T 之前的部分，-Txxx 为可选，当，出现 -T 时，-A 为可选，但 -A 不能单独出现）。
         * 可能的格式为：
         * 2023.12.14.1-T1-A123，
         * 2023.12.14.2-T1.1-A123
         * 2023.12.14.3-T2.1-A123
         * 2023.12.14.1-T2.2-A123
         * 2023.12.14.1-T2.2.1-A123
         * ...（T 后面可以有多个小数点）
         * 2023.12.14.1
         * 2023.12.14.2-T1.1
         */
        return Regex.IsMatch(versionOrNickName, @"^\d{4}\.\d{2}\.\d{2}\.\d+(-T\d+(\.\d+)*(-A\d+)?)?$");
    }

    /// <summary>
    /// 判断 Version 是否能匹配前缀
    /// </summary>
    /// <param name="compareString"></param>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static bool IsValidSegment(string compareString, string inputString)
    {
        // 完整正则表达式  
        string pattern = @"^\d{4}\.\d{2}\.\d{2}\.\d+(-T\d+(\.\d+)*(-A\d+)?)?$";
        Regex fullRegex = new Regex(pattern);

        // 检查输入字符串和对比字符串是否完全匹配  
        if (compareString == inputString)
        {
            return true;
        }

        // 检查输入字符串是否部分匹配对比字符串  
        if (!fullRegex.IsMatch(compareString))
        {
            return false;
        }

        // 分割对比字符串和输入字符串  
        string[] compareParts = compareString.Split('-');
        string[] inputParts = inputString.Split('-');

        // 输入部分段数不能超过对比字符串段数  
        if (inputParts.Length > compareParts.Length)
        {
            return false;
        }

        // 检查每个段是否匹配，并处理-T部分的继承规则  
        for (int i = 0; i < inputParts.Length; i++)
        {
            if (i == 0)
            {
                // 比较日期段  
                if (compareParts[i] != inputParts[i])
                {
                    return false;
                }
            }
            else if (inputParts[i].StartsWith("T"))
            {
                // 处理-T部分的继承规则  
                string[] compareTParts = compareParts[i].Substring(1).Split('.');
                string[] inputTParts = inputParts[i].Substring(1).Split('.');

                // 输入的T部分不能比对比的T部分更长  
                if (inputTParts.Length > compareTParts.Length)
                {
                    return false;
                }

                // 比较每一级T部分  
                for (int j = 0; j < inputTParts.Length; j++)
                {
                    if (compareTParts[j] != inputTParts[j])
                    {
                        return false;
                    }
                }
            }
            else
            {
                // 其他部分必须完全匹配  
                if (compareParts[i] != inputParts[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion


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

    public PromptItem UpdateExpectedResultsJson(string expectedResultsJson, bool overwrite = true)
    {
        if (!overwrite && !string.IsNullOrEmpty(this.ExpectedResultsJson))
        {
            return this;
        }
        else
        {
            this.ExpectedResultsJson = expectedResultsJson;

            return this;
        }
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
    /// <param name="variableDictJson"></param>
    /// <returns></returns>
    public PromptItem UpdateModelParam(float topP, float temperature, int maxToken, float frequencyPenalty, float presencePenalty,
        string stopSequences, string variableDictJson)
    {
        TopP = topP;
        Temperature = temperature;
        MaxToken = maxToken;
        FrequencyPenalty = frequencyPenalty;
        PresencePenalty = presencePenalty;
        StopSequences = stopSequences == "[]" ? null : stopSequences;

        VariableDictJson = variableDictJson;

        return this;
    }

    public PromptItem UpdateVariablesJson(string variablesJson, string prefix, string suffix)
    {
        VariableDictJson = variablesJson;
        Prefix = prefix;
        Suffix = suffix;

        return this;
    }

    public PromptItem UpdateContent(string skPrompt)
    {
        Content = skPrompt;

        return this;
    }

    public PromptItem UpdateDraft(PromptItemDto dto)
    {
        RangeId = dto.RangeId;
        RangeName = dto.RangeName;

        Content = dto.Content;

        ModelId = dto.ModelId;
        TopP = dto.TopP;
        Temperature = dto.Temperature;
        MaxToken = dto.MaxToken;
        FrequencyPenalty = dto.FrequencyPenalty;
        PresencePenalty = dto.PresencePenalty;
        StopSequences = dto.StopSequences;

        Note = dto.Note;

        ExpectedResultsJson = dto.ExpectedResultsJson;

        Prefix = dto.Prefix;
        Suffix = dto.Suffix;
        VariableDictJson = dto.VariableDictJson;

        return this;
    }

    /// <summary>
    /// 从 <see cref="VariableDictJson"/> 获取 <see cref="InputVariable"/> 对象
    /// </summary>
    /// <returns></returns>
    public IEnumerable<InputVariable> GetInputValiableObject()
    {
        var inputVariable = new List<InputVariable>();
        if (this.VariableDictJson.IsNullOrEmpty())
        {
            return inputVariable;
        }

        Dictionary<string, string> variablesDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(this.VariableDictJson);
        foreach (var item in variablesDictionary)
        {
            inputVariable.Add(new InputVariable()
            {
                Name = item.Key,
                Description = item.Key,
                Default = ""
            });
        }

        return inputVariable;
    }

    /// <summary>
    /// 当存在 <see cref="NickName"/> 时返回此属性，否则使用 <see cref="FullVersion"/>
    /// </summary>
    /// <returns></returns>
    public string GetAvailableName()
    {
        if (!this.NickName.IsNullOrEmpty())
        {
            return this.NickName;
        }
        else
        {
            return this.FullVersion;
        }
    }


}
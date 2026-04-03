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
using System.Linq;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

/// <summary>
///PromptItem: Prompt configuration for each different version
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(PromptItem))] /*The prefix must be added to prevent conflicts system-wide.*/
[Serializable]
public class PromptItem : EntityBase<int>
{
    /// <summary>
    ///rangeID
    /// </summary>
    public int RangeId { get; private set; }

    /// <summary>
    /// Nick name
    /// </summary>
    [MaxLength(50)]
    public string NickName { get; private set; }

    /// <summary>
    ///Prompt content
    /// </summary>
    public string Content { get; private set; }

    #region 模型参数

    /// <summary>
    /// AiModel.Id of the AI ​​model
    /// </summary>
    [Required]
    public int ModelId { get; private set; }

    /// <summary>
    /// TopP
    /// </summary>
    [Required]
    public float TopP { get; private set; }

    /// <summary>
    /// temperature
    /// </summary>
    [Required]
    public float Temperature { get; private set; }

    /// <summary>
    ///Maximum number of Tokens
    /// </summary>
    [Required]
    public int MaxToken { get; private set; }

    /// <summary>
    /// frequency penalty
    /// </summary>
    [Required]
    public float FrequencyPenalty { get; private set; }

    [Required] public float PresencePenalty { get; private set; }

    /// <summary>
    /// stop sequence (JSON array)
    /// </summary>
    public string StopSequences { get; private set; }

    #endregion

    #region 打分

    /// <summary>
    /// evaluation parameters, average score
    /// </summary>
    [MaxLength(3)]
    public decimal EvalAvgScore { get; private set; } = -1;

    /// <summary>
    ///evaluation parameters
    /// </summary>
    [MaxLength(3)]
    public decimal EvalMaxScore { get; private set; } = -1;

    /// <summary>
    ///Expected result Json
    /// </summary>
    public string ExpectedResultsJson { get; private set; }

    public bool IsAIGrade { get; private set; } = false;


    #endregion

    #region Full Version

    /// <summary>
    /// <para>Version number, in the format Name-Tactic-Aiming</para> 
    /// <example>2023.12.14.1-T1.1-A123</example>
    /// <para>Name: <inheritdoc cref="RangeName"/></para>
    /// <para>Tactic: <inheritdoc cref="Tactic"/></para>
    /// <para>Aiming: <inheritdoc cref="Aiming"/></para>
    /// is Tx where x is the branch number, str, 1.1.1 is allowed. . .
    /// Aiming is Ax where x is the number of shooting times, int
    /// </summary>
    [Required, MaxLength(50)]
    public string FullVersion
    {
        get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
        private set { }
    }

    /// <summary>
    /// Range name -> should use the name in PromptRange
    /// <para>The format is yyyy.MM.dd.x, where x is the serial number generated on the day, int</para>
    /// <example>Example 1
    ///     <code>2023.12.14.1</code>
    /// </example>
    /// <example>Example 2
    ///     <code>2023.12.14.123</code>
    /// </example>
    /// </summary>
    [MaxLength(20)]
    public string RangeName { get; private set; }

    /// <summary>
    /// Target lane name
    /// <para>The format is x.x.x..., where x is the branch number</para>
    /// <para>In the complete version number, -T should be used to connect Name</para>
    /// 
    /// </summary>
    public string Tactic { get; private set; }

    /// <summary>
    /// <para>is the number of target shooting, int</para>
    /// </summary>
    [MaxLength(5)]
    public int Aiming { get; private set; }

    /// <summary>
    /// Parent Tactic, can be an empty string
    /// </summary>
    [Required]
    public string ParentTac { get; private set; }

    #endregion

    #region Prompt请求参数

    /// <summary>
    /// prefix
    /// </summary>
    [MaxLength(10)]
    public string Prefix { get; private set; }

    /// <summary>
    /// suffix
    /// </summary>
    [MaxLength(10)]
    public string Suffix { get; private set; }

    /// <summary>
    /// Request parameter key-value pair JSON 
    /// </summary>

    public string VariableDictJson { get; private set; }

    #endregion

    /// <summary>
    ///Note (optional)
    /// </summary>
    [MaxLength(20)]
    public string Note { get; private set; }

    /// <summary>
    ///Last run time
    /// </summary>
    public DateTime LastRunTime { get; private set; } = DateTime.Now;

    /// <summary>
    /// Is it public?
    /// </summary>
    public bool IsShare { get; private set; } = false;

    public bool IsDraft { get; private set; }

    #region 静态方法

    /// <summary>
    /// Determine whether it is Version format
    /// </summary>
    /// <returns></returns>
    public static bool IsPromptVersion(string versionOrNickName)
    {
        /* Determine whether it is a Prompt version (from left to right, the part before -T must be included, -Txxx is optional, when -T appears, -A is optional, but -A cannot appear alone).
         * Possible formats are:
         * 2023.12.14.1-T1-A123，
         * 2023.12.14.2-T1.1-A123
         * 2023.12.14.3-T2.1-A123
         * 2023.12.14.1-T2.2-A123
         * 2023.12.14.1-T2.2.1-A123
         * ...(T can have multiple decimal points after it)
         * 2023.12.14.1
         * 2023.12.14.2-T1.1
         */
        return Regex.IsMatch(versionOrNickName, @"^\d{4}\.\d{2}\.\d{2}\.\d+(-T\d+(\.\d+)*(-A\d+)?)?$");
    }

    /// <summary>
    /// Determine whether Version can match the prefix
    /// </summary>
    /// <param name="compareString"></param>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static bool IsValidVersionSegment(string compareString, string inputString)
    {
        // Complete regular expression  
        string pattern = @"^\d{4}\.\d{2}\.\d{2}\.\d+(-T\d+(\.\d+)*(-A\d+)?)?$";
        Regex fullRegex = new Regex(pattern);

        // Check if input string and comparison string match exactly  
        if (compareString == inputString)
        {
            return true;
        }

        // Check if the input string partially matches the comparison string  
        if (!fullRegex.IsMatch(compareString))
        {
            return false;
        }

        // Split comparison string and input string  
        string[] compareParts = compareString.Split('-');
        string[] inputParts = inputString.Split('-');

        // The number of input part segments cannot exceed the number of comparison string segments.  
        if (inputParts.Length > compareParts.Length)
        {
            return false;
        }

        // Checks each segment for a match and handles inheritance rules for the -T part  
        for (int i = 0; i < inputParts.Length; i++)
        {
            if (i == 0)
            {
                // Compare date range  
                if (compareParts[i] != inputParts[i])
                {
                    return false;
                }
            }
            else if (inputParts[i].StartsWith("T"))
            {
                // Inheritance rules for handling -T parts  
                string[] compareTParts = compareParts[i].Substring(1).Split('.');
                string[] inputTParts = inputParts[i].Substring(1).Split('.');

                // The T part of the input cannot be longer than the T part of the comparison  
                if (inputTParts.Length > compareTParts.Length)
                {
                    return false;
                }

                // Compare T-sections at each level  
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
                // Other parts must match exactly  
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
    /// used when importing
    /// </summary>
    /// <param name="promptRange"></param>
    /// <param name="nickName"></param>
    /// <param name="tactic"></param>
    public PromptItem(PromptRangeDto promptRange, string nickName, string tactic)
    {
        RangeId = promptRange.Id;
        RangeName = promptRange.RangeName;
        Tactic = tactic;
        ParentTac = "";
        Aiming = 1;
        IsDraft = true;
        NickName = nickName;
    }

    public PromptItem(PromptItemDto dto) : this(dto.RangeId, dto.Content, dto.ModelId, dto.TopP, dto.Temperature, dto.MaxToken,
        dto.FrequencyPenalty, dto.PresencePenalty, dto.StopSequences, dto.RangeName, dto.Tactic, dto.Aiming, dto.ParentTac, dto.Note,
        dto.IsDraft, dto.ExpectedResultsJson, dto.isAIGrade,
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
        string note, bool isDraft, string expectedResultsJson, bool isAIGrade,
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
        IsAIGrade = isAIGrade;
        Prefix = prefix;
        Suffix = suffix;
        VariableDictJson = variableDictJson;
        EvalAvgScore = -1;
        EvalMaxScore = -1;
        
        // Initialize NickName (to avoid null causing database saving failure)
        NickName = string.Empty;
    }

    public PromptItem(int rangeId, string rangeName, string tactic, int aiming, string parentTac, PromptItem_AddRequest request) :
        this(rangeId, request.Content, request.ModelId, request.TopP, request.Temperature, request.MaxToken, request.FrequencyPenalty,
            request.PresencePenalty, request.StopSequences, rangeName, tactic, aiming, parentTac, request.Note, request.IsDraft,
            request.ExpectedResultsJson, request.isAIGrade,
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
    /// Update the model parameters used
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
        IsAIGrade = dto.isAIGrade;

        Prefix = dto.Prefix;
        Suffix = dto.Suffix;
        VariableDictJson = dto.VariableDictJson;

        return this;
    }

    /// <summary>
    /// Get the <see cref="InputVariable"/> object from <see cref="VariableDictJson"/>
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
    /// Return this property when <see cref="NickName"/> is present, otherwise use <see cref="FullVersion"/>
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


    /// <summary>
    /// Get RangeName, Tactic, Aim parameters based on FullVersion string
    /// </summary>
    /// <param name="fullVersion"></param>
    /// <returns></returns>
    public static PromptItemVersion GetVersionObject(string fullVersion)
    {
        string[] parts = fullVersion.Split(new[] { "-T", "-A" }, StringSplitOptions.RemoveEmptyEntries);

        string rangeName = parts[0];
        string tactic = parts[1];
        int aim = int.Parse(parts[2]);
        return new(rangeName, tactic, aim);
    }

    /// <summary>
    /// Determine the previous level Tastic from the Tastic string. If it is already the top level, return ""
    /// </summary>
    /// <param name="tastic"></param>
    /// <returns></returns>
    public static string GetParentTasticFromTastic(string tastic)
    {
        var tasticArr = tastic.Split('.');
        if (tasticArr.Length == 1)
        {
            return "";
        }
        else
        {
            var newTasticArr = tasticArr.Take(tasticArr.Length - 1).ToArray();
            return string.Join(".", newTasticArr);
        }
    }
}
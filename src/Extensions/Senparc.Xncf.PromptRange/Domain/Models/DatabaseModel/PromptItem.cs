/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem.cs
    File description: PromptItem related implementation

    Created by: Senparc - 20231003

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261105
    Change description: v0.20.14 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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
/// Stores one prompt-range tactic, its model parameters, version segments, and evaluation state.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(PromptItem))] /*a prefix is required to prevent system-wide conflicts*/
[Serializable]
public class PromptItem : EntityBase<int>
{
    /// <summary>
    /// range ID
    /// </summary>
    public int RangeId { get; private set; }

    /// <summary>
    /// display name
    /// </summary>
    [MaxLength(50)]
    public string NickName { get; private set; }

    /// <summary>
    /// Promptcontent
    /// </summary>
    public string Content { get; private set; }

    #region Legacy section

    /// <summary>
    /// Identifier of the AI model used to execute this prompt item.
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
    /// maximum token count
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
    /// Serialized stop-sequence configuration used by the model invocation.
    /// </summary>
    public string StopSequences { get; private set; }

    #endregion

    #region Legacy section

    /// <summary>
    /// Average evaluation score assigned to this prompt item.
    /// </summary>
    [MaxLength(3)]
    public decimal EvalAvgScore { get; private set; } = -1;

    /// <summary>
    /// Maximum evaluation score assigned to this prompt item.
    /// </summary>
    [MaxLength(3)]
    public decimal EvalMaxScore { get; private set; } = -1;

    public decimal? RobotScore { get; private set; }
    public decimal? HumanScore { get; private set; }
    public decimal? FinalScore { get; private set; }

    /// <summary>
    /// Records a validated reference score and mirrors it to the final score.
    /// </summary>
    /// <param name="score">A score from 0 through 10 with at most one decimal place.</param>
    /// <param name="isRobot">Whether the score was produced by the robot evaluator.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="score"/> is outside the supported precision/range.</exception>
    public void SetReferenceScore(decimal score, bool isRobot)
    {
        if (score < 0 || score > 10 || decimal.Round(score, 1) != score)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be 0-10 with at most one decimal place.");
        if (isRobot) RobotScore = score;
        else HumanScore = score;
        FinalScore = score;
        SetUpdateTime();
    }

    /// <summary>
    /// JSON representation of expected evaluation results.
    /// </summary>
    public string ExpectedResultsJson { get; private set; }

    public bool IsAIGrade { get; private set; } = false;


    #endregion

    #region Full Version

    /// <summary>
    /// Calculated version string combining range name, tactic, and aiming segments.
    /// <example>2023.12.14.1-T1.1-A123</example>
    /// <para>Name: <inheritdoc cref="RangeName"/></para>
    /// <para>Tactic: <inheritdoc cref="Tactic"/></para>
    /// <para>Aiming: <inheritdoc cref="Aiming"/></para>
    /// </summary>
    [Required, MaxLength(50)]
    public string FullVersion
    {
        get { return $"{RangeName}-T{Tactic}-A{Aiming}"; }
        private set { }
    }

    /// <summary>
    /// Prompt range name; use the name defined in PromptRange.
    /// <para>Format: yyyy.MM.dd.x, where x is the sequence number generated on that day.</para>
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
    /// Tactic segment of the prompt version, such as <c>1.1</c>.
    /// </summary>
    public string Tactic { get; private set; }

    /// <summary>
    /// Numeric aiming segment of the prompt version.
    /// </summary>
    [MaxLength(5)]
    public int Aiming { get; private set; }

    /// <summary>
    /// Parent tactic identifier used to represent tactic hierarchy.
    /// </summary>
    [Required]
    public string ParentTac { get; private set; }

    #endregion

    #region Legacy section

    /// <summary>
    /// Optional text inserted before the prompt variable content.
    /// </summary>
    [MaxLength(10)]
    public string Prefix { get; private set; }

    /// <summary>
    /// Optional text appended after the prompt variable content.
    /// </summary>
    [MaxLength(10)]
    public string Suffix { get; private set; }

    /// <summary>
    /// requestparameterskey-value pair JSON 
    /// </summary>

    public string VariableDictJson { get; private set; }

    #endregion

    /// <summary>
    /// Note (optional)
    /// </summary>
    [MaxLength(20)]
    public string Note { get; private set; }

    /// <summary>
    /// Time at which this prompt item was last executed.
    /// </summary>
    public DateTime LastRunTime { get; private set; } = DateTime.Now;

    /// <summary>
    /// Indicates whether the prompt item is available for sharing.
    /// </summary>
    public bool IsShare { get; private set; } = false;

    public bool IsDraft { get; private set; }

    #region Legacy section

    /// <summary>
    /// Determines whether text matches the supported prompt-version format.
    /// </summary>
    /// <param name="versionOrNickName">The full version or nickname candidate.</param>
    /// <returns><see langword="true"/> when the value has a date, sequence, and optional tactic/aiming suffix.</returns>
    public static bool IsPromptVersion(string versionOrNickName)
    {
        // Accepted examples include 2023.12.14.1, 2023.12.14.2-T1.1,
        // and 2023.12.14.1-T2.2.1-A123.
        return Regex.IsMatch(versionOrNickName, @"^\d{4}\.\d{2}\.\d{2}\.\d+(-T\d+(\.\d+)*(-A\d+)?)?$");
    }

    /// <summary>
    /// Determines whether an input version segment is equal to or a valid prefix of a complete version.
    /// </summary>
    /// <param name="compareString">The complete version string to compare against.</param>
    /// <param name="inputString">The version or partial version to validate.</param>
    /// <returns><see langword="true"/> when the input is an accepted matching segment.</returns>
    public static bool IsValidVersionSegment(string compareString, string inputString)
    {
        string pattern = @"^\d{4}\.\d{2}\.\d{2}\.\d+(-T\d+(\.\d+)*(-A\d+)?)?$";
        Regex fullRegex = new Regex(pattern);

        if (compareString == inputString)
        {
            return true;
        }

        if (!fullRegex.IsMatch(compareString))
        {
            return false;
        }

        string[] compareParts = compareString.Split('-');
        string[] inputParts = inputString.Split('-');

        if (inputParts.Length > compareParts.Length)
        {
            return false;
        }

        // Compare the base range segment, tactic hierarchy, and optional aiming segment in order.
        for (int i = 0; i < inputParts.Length; i++)
        {
            if (i == 0)
            {
                if (compareParts[i] != inputParts[i])
                {
                    return false;
                }
            }
            else if (inputParts[i].StartsWith("T"))
            {
                string[] compareTParts = compareParts[i].Substring(1).Split('.');
                string[] inputTParts = inputParts[i].Substring(1).Split('.');

                if (inputTParts.Length > compareTParts.Length)
                {
                    return false;
                }

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
                if (compareParts[i] != inputParts[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion


    #region Legacy section

    /// <summary>
    /// Initializes a draft prompt item from a prompt range.
    /// </summary>
    /// <param name="promptRange">The prompt range providing the identifier and name.</param>
    /// <param name="nickName">The optional display nickname.</param>
    /// <param name="tactic">The tactic segment used to build the full version.</param>
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

    /// <summary>
    /// Initializes a prompt item from its DTO and preserves evaluation/runtime fields.
    /// </summary>
    /// <param name="dto">The source DTO.</param>
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

    /// <summary>
    /// Initializes a prompt item from its complete persisted field set.
    /// </summary>
    /// <param name="rangeId">The owning prompt-range identifier.</param>
    /// <param name="content">The Semantic Kernel prompt content.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="topP">The nucleus-sampling value.</param>
    /// <param name="temperature">The sampling temperature.</param>
    /// <param name="maxToken">The maximum token count.</param>
    /// <param name="frequencyPenalty">The frequency penalty.</param>
    /// <param name="presencePenalty">The presence penalty.</param>
    /// <param name="stopSequences">The serialized stop sequences.</param>
    /// <param name="rangeName">The prompt-range version name.</param>
    /// <param name="tactic">The tactic segment.</param>
    /// <param name="aiming">The aiming number.</param>
    /// <param name="parentTac">The parent tactic identifier.</param>
    /// <param name="note">The optional note.</param>
    /// <param name="isDraft">Whether the item is a draft.</param>
    /// <param name="expectedResultsJson">The expected-results JSON.</param>
    /// <param name="isAIGrade">Whether grading is AI-based.</param>
    /// <param name="prefix">The prompt variable prefix.</param>
    /// <param name="suffix">The prompt variable suffix.</param>
    /// <param name="variableDictJson">The prompt variable dictionary JSON.</param>
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
        
        // A full-field constructor starts without a user-facing nickname.
        NickName = string.Empty;
    }

    /// <summary>
    /// Initializes a prompt item from an add request and externally supplied range/version parts.
    /// </summary>
    /// <param name="rangeId">The owning prompt-range identifier.</param>
    /// <param name="rangeName">The prompt-range version name.</param>
    /// <param name="tactic">The tactic segment.</param>
    /// <param name="aiming">The aiming number.</param>
    /// <param name="parentTac">The parent tactic identifier.</param>
    /// <param name="request">The request containing prompt content and model settings.</param>
    public PromptItem(int rangeId, string rangeName, string tactic, int aiming, string parentTac, PromptItem_AddRequest request) :
        this(rangeId, request.Content, request.ModelId, request.TopP, request.Temperature, request.MaxToken, request.FrequencyPenalty,
            request.PresencePenalty, request.StopSequences, rangeName, tactic, aiming, parentTac, request.Note, request.IsDraft,
            request.ExpectedResultsJson, request.isAIGrade,
            request.Prefix, request.Suffix, request.VariableDictJson)
    {
    }

    #endregion

    /// <summary>
    /// Updates expected-result JSON, optionally preserving an existing non-empty value.
    /// </summary>
    /// <param name="expectedResultsJson">The expected-results JSON to store.</param>
    /// <param name="overwrite">Whether an existing non-empty value may be replaced.</param>
    /// <returns>This item for fluent updates.</returns>
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

    /// <summary>
    /// Sets whether the prompt item is shared.
    /// </summary>
    /// <param name="show">The desired sharing state.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem ShareSwitch(bool show)
    {
        this.IsShare = show;

        return this;
    }

    /// <summary>
    /// Replaces the prompt item's display nickname.
    /// </summary>
    /// <param name="nickName">The new nickname.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem ModifyNickName(string nickName)
    {
        this.NickName = nickName;

        return this;
    }

    /// <summary>
    /// Stores the average evaluation score without applying range validation.
    /// </summary>
    /// <param name="score">The evaluation score.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem UpdateEvalAvgScore(decimal score)
    {
        this.EvalAvgScore = score;

        return this;
    }

    /// <summary>
    /// Stores the maximum evaluation score without applying range validation.
    /// </summary>
    /// <param name="score">The evaluation score.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem UpdateEvalMaxScore(decimal score)
    {
        this.EvalMaxScore = score;

        return this;
    }

    /// <summary>
    /// Replaces the optional prompt note.
    /// </summary>
    /// <param name="note">The note to store.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem ModifyNote(string note)
    {
        this.Note = note;

        return this;
    }

    /// <summary>
    /// Sets the draft flag.
    /// </summary>
    /// <param name="isDraft">The desired draft state.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem DraftSwitch(bool isDraft)
    {
        this.IsDraft = isDraft;

        return this;
    }

    /// <summary>
    /// Replaces model sampling and stop-sequence parameters.
    /// </summary>
    /// <param name="topP">The nucleus-sampling value.</param>
    /// <param name="temperature">The sampling temperature.</param>
    /// <param name="maxToken">The maximum token count.</param>
    /// <param name="frequencyPenalty">The frequency penalty.</param>
    /// <param name="presencePenalty">The presence penalty.</param>
    /// <param name="stopSequences">The serialized stop sequences; the literal empty array is stored as null.</param>
    /// <param name="variableDictJson">The variable dictionary JSON.</param>
    /// <returns>This item for fluent updates.</returns>
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

    /// <summary>
    /// Replaces the variable dictionary and its prompt prefix/suffix.
    /// </summary>
    /// <param name="variablesJson">The variable dictionary JSON.</param>
    /// <param name="prefix">The prompt variable prefix.</param>
    /// <param name="suffix">The prompt variable suffix.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem UpdateVariablesJson(string variablesJson, string prefix, string suffix)
    {
        VariableDictJson = variablesJson;
        Prefix = prefix;
        Suffix = suffix;

        return this;
    }

    /// <summary>
    /// Replaces the Semantic Kernel prompt content.
    /// </summary>
    /// <param name="skPrompt">The new prompt content.</param>
    /// <returns>This item for fluent updates.</returns>
    public PromptItem UpdateContent(string skPrompt)
    {
        Content = skPrompt;

        return this;
    }

    /// <summary>
    /// Copies editable draft fields from a DTO while retaining the entity identity.
    /// </summary>
    /// <param name="dto">The DTO containing draft values.</param>
    /// <returns>This item for fluent updates.</returns>
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
    /// Parses the JSON variable dictionary into input-variable descriptors.
    /// </summary>
    /// <returns>The input variables, or an empty sequence when no dictionary is configured.</returns>
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
    /// Returns the nickname when present, otherwise the calculated full prompt version.
    /// </summary>
    /// <returns>The user-facing prompt item name.</returns>
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
    /// Parses a full prompt version into its range name, tactic, and aiming components.
    /// </summary>
    /// <param name="fullVersion">A value formatted as range name, tactic, and aiming segments.</param>
    /// <returns>The parsed prompt item version.</returns>
    /// <exception cref="FormatException">Thrown when the aiming segment is not an integer.</exception>
    public static PromptItemVersion GetVersionObject(string fullVersion)
    {
        string[] parts = fullVersion.Split(new[] { "-T", "-A" }, StringSplitOptions.RemoveEmptyEntries);

        string rangeName = parts[0];
        string tactic = parts[1];
        int aim = int.Parse(parts[2]);
        return new(rangeName, tactic, aim);
    }

    /// <summary>
    /// Removes the final dot-separated segment from a tactic identifier.
    /// </summary>
    /// <param name="tastic">The tactic identifier to shorten.</param>
    /// <returns>The parent tactic, or an empty string for a root tactic.</returns>
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

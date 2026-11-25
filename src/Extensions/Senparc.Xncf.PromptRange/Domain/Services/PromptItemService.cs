/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItemService.cs
    File description: Related implementation.

    Created by: Senparc - 20231021

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Models.Entities;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public partial class PromptItemService : ServiceBase<PromptItem>
{
    private readonly AIModelService _aiModelService;
    private readonly PromptRangeService _promptRangeService;
    // private readonly PromptResultService _promptResultService;

    /// <summary>
    /// Initializes PromptItem persistence and its PromptRange/model dependencies.
    /// </summary>
    /// <param name="repo">Repository for prompt-item records.</param>
    /// <param name="serviceProvider">Dependency-injection provider used by the base service.</param>
    /// <param name="aiModelService">Service used to resolve model metadata.</param>
    /// <param name="promptRangeService">Service used to resolve PromptRange metadata.</param>
    public PromptItemService(
        IRepositoryBase<PromptItem> repo,
        IServiceProvider serviceProvider,
        AIModelService aiModelService,
        PromptRangeService promptRangeService
    ) : base(repo, serviceProvider)
    {
        _aiModelService = aiModelService;
        _promptRangeService = promptRangeService;
    }

    /// <summary>
    /// Retrieves one prompt item directly for test or diagnostic callers.
    /// </summary>
    /// <param name="id">The prompt-item identifier.</param>
    /// <returns>A task containing the matching prompt item, or the default value.</returns>
    public async Task<PromptItem> ForUnitTest(int id)
    {
        return await base.RepositoryBase.GetFirstOrDefaultObjectAsync(z => z.Id == id);
    }

    /// <summary>
    /// Creates or derives a PromptItem version from a request, validating the requested
    /// tactic/aiming branch and rejecting duplicate full-version identifiers.
    /// </summary>
    /// <param name="request">Prompt content, range, tactic, scoring, and generation options.</param>
    /// <returns>A task containing the persisted prompt-item DTO.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the range is invalid, the request selects conflicting branches, or the generated version already exists.</exception>
    public async Task<PromptItemDto> AddPromptItemAsync(PromptItem_AddRequest request)
    {
        #region validate request dto

        //Legacy comment translated to English.
        if (request.IsTopTactic && request.IsNewTactic && request.IsNewSubTactic)
        {
            throw new NcfExceptionBase("IsTopTactic IsNewTactic IsNewSubTactic不能同时为True");
        }

        //Legacy comment translated to English.
        request.MaxToken = request.MaxToken > 0 ? request.MaxToken : 2000;
        request.StopSequences = request.StopSequences == "" ? null : request.StopSequences;

        if (request.RangeId <= 0)
        {
            throw new NcfExceptionBase($"RangeId必须为正整数，当前是{request.RangeId}");
        }

        #endregion

        #region Legacy section

        var promptRange = await _promptRangeService.GetAsync(request.RangeId);

        #endregion

        //Legacy comment translated to English.
        // var today = SystemTime.Now;
        // var todayStr = today.ToString("yyyy.MM.dd");

        #region Legacy section

        PromptItem toSavePromptItem;
        if (request.Id == null)
        {
            //Legacy comment translated to English.
            toSavePromptItem = new PromptItem(
                rangeId: promptRange.Id,
                rangeName: promptRange.RangeName, // $"{todayStr}.{todayPromptList.Count + 1}",
                tactic: "1",
                aiming: 1,
                parentTac: "",
                request: request
            );
        }
        else
        {
            //Legacy comment translated to English.
            // var basePrompt = await base.GetObjectAsync(p => p.Id == request.Id);
            var basePrompt = await this.GetAsync(request.Id.Value);
            // var promptRange = await _promptRangeService.GetObjectAsync(r => r.Id == basePrompt.RangeId);

            string rangeName = basePrompt.RangeName;

            if (request.IsTopTactic)
            {
                //Legacy comment translated to English.
                var parentTac = "";
                List<PromptItem> fullList = await base.GetFullListAsync(p =>
                    p.RangeName == rangeName &&
                    p.ParentTac == parentTac &&
                    p.FullVersion.EndsWith("A1")
                );

                var maxTactic = fullList.Count == 0
                    ? 0
                    : fullList.Select(p => int.Parse(p.Tactic)).Max();
                toSavePromptItem = new PromptItem(
                    rangeId: basePrompt.RangeId,
                    rangeName: rangeName,
                    tactic: $"{maxTactic + 1}",
                    aiming: 1,
                    parentTac: parentTac,
                    request: request
                );
                // copy the expected result here
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewTactic)
            {
                //Legacy comment translated to English.
                var parentTac = basePrompt.ParentTac;
                List<PromptItem> fullList = await base.GetFullListAsync(p =>
                    p.RangeName == rangeName &&
                    p.ParentTac == parentTac && p.FullVersion.EndsWith("A1")
                );

                var maxTactic = fullList.Count == 0
                    ? 0
                    : fullList.Select(p => p.Tactic.Substring((parentTac == "" ? "" : parentTac + ".").Length))
                        .Select(int.Parse)
                        .Max();

                var tactic = (parentTac == "" ? "" : parentTac + ".") + $"{maxTactic + 1}";

                toSavePromptItem = new PromptItem(
                    rangeId: basePrompt.RangeId,
                    rangeName: rangeName,
                    tactic: tactic,
                    aiming: 1,
                    parentTac: parentTac,
                    request: request
                );
                // copy the expected result here
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewSubTactic)
            {
                //Legacy comment translated to English.
                var parentTac = basePrompt.Tactic;
                List<PromptItem> fullList = await base.GetFullListAsync(
                    p => p.RangeName == rangeName &&
                         p.ParentTac == parentTac
                         && p.FullVersion.EndsWith("A1")
                );

                var maxTactic = fullList.Count == 0
                    ? 0
                    : fullList.Select(p => p.Tactic.Substring((parentTac + ".").Length))
                        .Select(int.Parse)
                        .Max();

                toSavePromptItem = new PromptItem(
                    rangeId: basePrompt.RangeId,
                    rangeName: rangeName,
                    tactic: $"{parentTac}.{maxTactic + 1}",
                    aiming: 1,
                    parentTac: parentTac,
                    request: request
                );
                // copy the expected result here
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewAiming) //Legacy comment translated to English.
            {
                List<PromptItem> fullList = await base.GetFullListAsync(p =>
                    // p.FullVersion.StartsWith(oldPrompt.FullVersion.Substring(0, oldPrompt.FullVersion.LastIndexOf('A')))
                    p.FullVersion.StartsWith($"{basePrompt.RangeName}-T{basePrompt.Tactic}-A")
                );

                var maxAiming = fullList.Count == 0 ? 0 : fullList.Select(p => p.Aiming).Max();
                toSavePromptItem = new PromptItem(
                    rangeId: basePrompt.RangeId,
                    rangeName: rangeName,
                    tactic: basePrompt.Tactic,
                    aiming: maxAiming + 1,
                    parentTac: basePrompt.ParentTac,
                    request: request
                );
                // copy the expected result here
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else
            {
                SenparcTrace.SendCustomLog("AddPrompt", "指示符都是false, 没有新增");
                return basePrompt;
                //Legacy comment translated to English.
                // var promptResultService = _serviceProvider.GetService<PromptResultService>();
                // var resultDto = await promptResultService.SenparcGenerateResultAsync(basePrompt);
            }
        }

        #endregion

        //Legacy comment translated to English.
        var existPromptItem = await base.GetObjectAsync(p => p.FullVersion == toSavePromptItem.FullVersion);
        if (existPromptItem != null)
        {
            throw new NcfExceptionBase("版号生成错误，请重新打靶");
        }

        await base.SaveObjectAsync(toSavePromptItem);

        return await this.TransEntityToDtoAsync(toSavePromptItem);
    }

    /// <summary>
    /// Builds the tactic tree rooted at the range and tactic of a prompt item.
    /// </summary>
    /// <param name="allPromptItems">The prompt items available for tree construction.</param>
    /// <param name="promptItem">The prompt item whose range identifies the tree.</param>
    /// <returns>A task containing tactic tree nodes.</returns>
    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(List<PromptItem> allPromptItems, PromptItem promptItem)
    {
        return await this.GenerateTacticTreeAsync(allPromptItems, promptItem.RangeName);
    }

    /// <summary>
    /// Loads the score history for all scored versions in the same prompt range as the item.
    /// </summary>
    /// <param name="promptItemId">The prompt-item identifier whose range history is requested.</param>
    /// <returns>A task containing version identifiers and their average and maximum scores.</returns>
    public async Task<PromptItem_HistoryScoreResponse> GetHistoryScoreAsync(int promptItemId)
    {
        var versionHistoryList = new List<string>();
        var avgScoreHistoryList = new List<decimal>();
        var maxScoreHistoryList = new List<decimal>();

        var curItem = await this.GetAsync(promptItemId);

        //Legacy comment translated to English.
        List<PromptItem> fullList = await this.GetFullListAsync(
            p => p.RangeName == curItem.RangeName && p.EvalAvgScore >= 0 && p.EvalMaxScore >= 0,
            p => p.Id,
            OrderingType.Ascending);

        //Legacy comment translated to English.
        foreach (var promptItem in fullList)
        {
            versionHistoryList.Add(promptItem.FullVersion);
            avgScoreHistoryList.Add(promptItem.EvalAvgScore);
            maxScoreHistoryList.Add(promptItem.EvalMaxScore);
        }

        return new PromptItem_HistoryScoreResponse(
            versionHistoryList,
            avgScoreHistoryList,
            maxScoreHistoryList
        );
    }

    /// <summary>
    /// Replaces the expected-result definition for one prompt item.
    /// </summary>
    /// <param name="promptItemId">The prompt-item identifier.</param>
    /// <param name="expectedResults">The serialized expected-result definition.</param>
    /// <returns>A task containing the updated prompt-item DTO.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the prompt item does not exist or the expected-result definition is invalid.</exception>
    public async Task<PromptItemDto> UpdateExpectedResultsAsync(int promptItemId, string expectedResults)
    {
        var promptItem = await this.GetObjectAsync(p => p.Id == promptItemId) ??
                         throw new Exception("未找到prompt");

        promptItem.UpdateExpectedResultsJson(expectedResults);

        await this.SaveObjectAsync(promptItem);

        return this.Mapper.Map<PromptItemDto>(promptItem);
    }

    /// <summary>
    /// Groups scored prompt items by top-level tactic and builds chart points using either
    /// average or maximum evaluation scores.
    /// </summary>
    /// <param name="promptItemId">The prompt-item identifier used to select the prompt range.</param>
    /// <param name="isAvg">Whether average scores are used instead of maximum scores.</param>
    /// <returns>A task containing the tactic-grouped chart response.</returns>
    /// <exception cref="Exception">Thrown when the referenced prompt item cannot be found.</exception>
    public async Task<Statistic_TodayTacticResponse> GetLineChartDataAsync(int promptItemId, bool isAvg)
    {
        var promptItem = await this.GetObjectAsync(p => p.Id == promptItemId) ??
                         throw new Exception("未找到prompt");


        //Legacy comment translated to English.
        List<PromptItemDto> promptItems = (await this.GetFullListAsync(
                p => p.RangeId == promptItem.RangeId && (isAvg ? p.EvalAvgScore >= 0 : p.EvalMaxScore >= 0),
                p => p.Id,
                OrderingType.Ascending)
            )
            .Select(p => this.Mapper.Map<PromptItemDto>(p))
            .ToList();

        //Legacy comment translated to English.
        var itemGroupByT = promptItems.GroupBy(p => p.Tactic.Split('.')[0])
            .ToDictionary(p => p.Key, p => p.ToList());

        var resp = new Statistic_TodayTacticResponse(promptItem.RangeName, DateTime.Now);

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        foreach (var (tac, itemList) in itemGroupByT)
        {
            var i = 0;
            var points = (
                from t in itemList
                let zScore = isAvg ? t.EvalAvgScore : t.EvalMaxScore
                select new Statistic_TodayTacticResponse.Point($"T{tac}", (++i).ToString(), zScore, t)
            ).ToList();
            //(
            //        from t in itemList
            //        let zScore = isAvg ? t.EvalAvgScore : t.EvalMaxScore
            //        select new Statistic_TodayTacticResponse.Point($"T{tac}", t.FullVersion, zScore, t)
            //    )
            //    .ToList();

            resp.DataPoints.Add(points);
        }

        return resp;
    }

    /// <summary>
    /// Retrieves a prompt item and maps it to the service DTO shape.
    /// </summary>
    /// <param name="id">The prompt-item identifier.</param>
    /// <returns>A task containing the prompt-item DTO.</returns>
    public async Task<PromptItemDto> GetAsync(int id)
    {
        var item = await this.GetObjectAsync(p => p.Id == id) ??
                   throw new NcfExceptionBase($"找不到{id}对应的promptItem");

        return await this.TransEntityToDtoAsync(item, needRange: true);
    }

    /// <summary>
    /// Changes the draft flag for a prompt item.
    /// </summary>
    /// <param name="id">The prompt-item identifier.</param>
    /// <param name="status">The new draft state.</param>
    /// <returns>A task containing the updated prompt-item DTO.</returns>
    public async Task<PromptItemDto> DraftSwitch(int id, bool status)
    {
        var promptItem = await this.GetObjectAsync(p => p.Id == id) ??
                         throw new NcfExceptionBase($"找不到{id}对应的靶道");

        promptItem.DraftSwitch(status);

        await this.SaveObjectAsync(promptItem);

        return await this.TransEntityToDtoAsync(promptItem);
    }

    /// <summary>
    /// Resolves a PromptRange version string and returns its item plus model/range metadata.
    /// </summary>
    /// <param name="promptRangeVersion">The full PromptRange version identifier.</param>
    /// <param name="isAvg">Whether average scoring data should be included.</param>
    /// <returns>A task containing the resolved version response.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the version format or corresponding prompt item is invalid.</exception>
    public async Task<SenparcAI_GetByVersionResponse> GetWithVersionAsync(string promptRangeVersion, bool isAvg = true)
    {
        var promptItem = await GetBestPromptAsync(promptRangeVersion, isAvg);

        var dto = await this.TransEntityToDtoAsync(promptItem); // this.Mapper.Map<PromptItemDto>(item);

        //var aiModel = await _aiModelService.GetObjectAsync(model => model.Id == dto.ModelId) ??
        //Legacy comment translated to English.

        //dto.AIModelDto = new AIModelDto(aiModel)
        //{
        //    ApiKey = aiModel.ApiKey,
        //    OrganizationId = aiModel.OrganizationId
        //};

        return new SenparcAI_GetByVersionResponse(_aiModelService.BuildSenparcAiSetting(dto.AIModelDto), dto);
    }


    /// <summary>
    /// Selects the best usable prompt item for a PromptRange version.
    /// </summary>
    /// <param name="promptRangeVersion">The full PromptRange version identifier.</param>
    /// <param name="isAvg">Whether average score selection is used.</param>
    /// <returns>A task containing the selected prompt item.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the version cannot be resolved to a prompt item.</exception>
    public async Task<PromptItem> GetBestPromptAsync(string promptRangeVersion, bool isAvg)
    {
        PromptItem promptItem;
        if (promptRangeVersion.Contains("-T") && promptRangeVersion.Contains("-A"))
        {
            //Legacy comment translated to English.
            promptItem = await this.GetObjectAsync(p => p.FullVersion == promptRangeVersion) ??
                         throw new NcfExceptionBase($"找不到 {promptRangeVersion} 对应的 PromptItem");
        }
        else
        {
            //Legacy comment translated to English.

            var searchTactic = promptRangeVersion.Contains("-T");

            var versionSet = promptRangeVersion.Split(new[] { "-T" }, StringSplitOptions.None);

            // validate rangeName
            var rangeName = versionSet[0];
            var promptRange = await _promptRangeService.GetObjectAsync(r => r.RangeName == rangeName)
                ?? throw new NcfExceptionBase($"找不到 {rangeName} 对应的靶场");

            var seh = new SenparcExpressionHelper<PromptItem>();
            seh.ValueCompare
                .AndAlso(true, z => z.RangeId == promptRange.Id/* z.RangeName == rangeName*/) //Legacy comment translated to English.
                .AndAlso(isAvg, z => z.EvalAvgScore >= 0) //Legacy comment translated to English.
                .AndAlso(!isAvg, z => z.EvalMaxScore >= 0); //Legacy comment translated to English.

            if (searchTactic)
            {
                //Legacy comment translated to English.
                var tactic = versionSet[1];
                seh.ValueCompare.AndAlso(true, z => z.Tactic == tactic);//Legacy comment translated to English.
            }
            else
            {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
            }

            //Legacy comment translated to English.
            var where = seh.BuildWhereExpression();

            //Legacy comment translated to English.
            promptItem = await this.GetObjectAsync(where,
                p => (isAvg ? p.EvalAvgScore : p.EvalMaxScore),
                OrderingType.Descending);
        }

        if (promptItem == null)
        {
            throw new NcfExceptionBase("找不到匹配条件的 PromptItem，请检查 PromptRange 的名称是否准确，或其靶场（PromptRange）下面的所有的 PromptItem 结果是否都未进行打分，系统必须选择一个评分最高的 PromptItem。");
        }

        return promptItem;
    }


    /// <summary>
    /// Maps a prompt item to its DTO and optionally loads its AI-model and prompt-range metadata.
    /// </summary>
    /// <param name="promptItem">The persisted prompt item to map.</param>
    /// <param name="needModel">Whether to resolve and attach AI-model metadata.</param>
    /// <param name="needRange">Whether to resolve and attach prompt-range metadata.</param>
    /// <returns>A task containing the mapped DTO.</returns>
    private async Task<PromptItemDto> TransEntityToDtoAsync(PromptItem promptItem, bool needModel = true, bool needRange = true)
    {
        var promptItemDto = this.Mapper.Map<PromptItemDto>(promptItem);

        #region Legacy section

        if (needModel)
        {
            var aiModel = await _aiModelService.GetObjectAsync(model => model.Id == promptItem.ModelId);
            //Legacy comment translated to English.
            if (aiModel == null)
            {
                SenparcTrace.SendCustomLog("NotFoundException", $"找不到{promptItem.ModelId}对应的AIModel");
            }
            else
            {
                promptItemDto.AIModelDto = new AIModelDto(aiModel)
                {
                    ApiKey = aiModel.ApiKey,
                    OrganizationId = aiModel.OrganizationId
                };
            }
        }

        #endregion

        #region Legacy section

        if (needRange)
        {
            var promptRangeDto = await _promptRangeService.GetAsync(promptItem.RangeId);
            promptItemDto.PromptRange = promptRangeDto;
        }

        #endregion

        // if (needResult)
        // {
        //     var promptResultService = _serviceProvider.GetService<PromptResultService>();
        //
        //     List<PromptResultDto> promptResultList = await promptResultService.GetByItemId(promptItem.Id);
        //     promptItemDto.PromptResultList = promptResultList;
        // }

        return promptItemDto;
    }

    /// <summary>
    /// Builds model configuration parameters from the persisted prompt-item settings.
    /// </summary>
    /// <param name="promptItem">The prompt-item DTO containing generation settings.</param>
    /// <returns>The normalized model configuration parameters.</returns>
    public PromptConfigParameter GetPromptConfigParameterFromAiSetting(PromptItemDto promptItem)
    {
        //Legacy comment translated to English.
        var promptParameter = new PromptConfigParameter()
        {
            MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 200,
            Temperature = promptItem.Temperature,
            TopP = promptItem.TopP,
            FrequencyPenalty = promptItem.FrequencyPenalty,
            PresencePenalty = promptItem.PresencePenalty,
            StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>() ?? new List<string>(),
        };

        return promptParameter;
    }

    /// <summary>
    /// Builds chat options from prompt-item generation settings and a system instruction.
    /// </summary>
    /// <param name="promptItemDto">The prompt-item DTO containing generation settings.</param>
    /// <param name="systemMessage">The system instruction passed to the chat model.</param>
    /// <returns>The configured chat options.</returns>
    public ChatOptions GetChatOptions(PromptItemDto promptItemDto, string systemMessage)
    {
        var chatOptions = new ChatOptions()
        {
            Instructions = systemMessage,
            MaxOutputTokens = promptItemDto.MaxToken,
            TopP = promptItemDto.TopP,
            Temperature = promptItemDto.Temperature,
            PresencePenalty = promptItemDto.PresencePenalty,
            FrequencyPenalty = promptItemDto.FrequencyPenalty,
            StopSequences = (promptItemDto.StopSequences ?? "[]").GetObject<List<string>>() ?? new List<string>()
        };
        return chatOptions;
    }

    #region Legacy section

    /// <summary>
    /// Builds a hierarchical tactic tree for all prompt items belonging to a prompt range.
    /// </summary>
    /// <param name="allPromptitems">The prompt items available for tree construction.</param>
    /// <param name="rangeName">The prompt-range name to include.</param>
    /// <returns>A task containing root tactic nodes and their children.</returns>
    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(List<PromptItem> allPromptitems, string rangeName)
    {
        //Legacy comment translated to English.
        List<PromptItem> fullList = allPromptitems.Where(p => p.RangeName == rangeName).ToList();
        //Console.WriteLine("fulllist:" + fullList.OrderBy(z=>z.Id).Select(z => new { z.Id, z.RangeName, z.Tactic, z.Aiming, z.FullVersion,z.ParentTac }).ToJson(true));

        //Legacy comment translated to English.
        var rootNodeList = new List<TreeNode<PromptItem_GetIdAndNameResponse>>();

        //Legacy comment translated to English.
        TreeNode<PromptItem_GetIdAndNameResponse> findNode(TreeNode<PromptItem_GetIdAndNameResponse> parentNode, string promptRange, string tascic)
        {
            if (parentNode == null)
            {
                return null;
            }

            if (parentNode.Data.RangeName == rangeName && parentNode.Data.PromptItemVersion.Tactic == tascic)
            {
                return parentNode;
            }

            //Legacy comment translated to English.
            for (int i = parentNode.Children.Count - 1; i >= 0; i--)
            {
                var item = parentNode.Children[i];
                var result = findNode(item, promptRange, tascic);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        for (int i = 0; i < fullList.Count; i++)
        {
            var promptitem = fullList[i];

            //Legacy comment translated to English.
            TreeNode<PromptItem_GetIdAndNameResponse> parentNode = null;
            for (int j = rootNodeList.Count - 1; j >= 0; j--)
            {
                var item = rootNodeList[j];
                parentNode = findNode(item, promptitem.RangeName, promptitem.ParentTac);
                if (parentNode != null)
                {
                    break;
                }
            }
        }

        foreach (var promptitem in fullList)
        {
            //Legacy comment translated to English.

            TreeNode<PromptItem_GetIdAndNameResponse> parentNode = null;
            for (int i = 0; i < rootNodeList.Count; i++)
            {
                var item = rootNodeList[i];
                parentNode = findNode(item, promptitem.RangeName, promptitem.ParentTac);
                if (parentNode != null)
                {
                    break;
                }
            }

            //Legacy comment translated to English.
            var newNode = new TreeNode<PromptItem_GetIdAndNameResponse>(promptitem.FullVersion, promptitem.NickName, new PromptItem_GetIdAndNameResponse(promptitem), -1);

            if (parentNode == null)
            {
                //Legacy comment translated to English.
                newNode.Level = 1;
                rootNodeList.Add(newNode);
            }
            else
            {
                //Legacy comment translated to English.
                newNode.Level = parentNode.Level + 1;
                parentNode.Children.Add(newNode);
            }
        }
        return rootNodeList;
    }


    /// <summary>
    /// Builds the selectable prompt-range/tactic tree used by the administration UI.
    /// </summary>
    /// <param name="showPromptRangeNode">Whether prompt-range root nodes are included.</param>
    /// <param name="showTacticNode">Whether top-level tactic nodes are included.</param>
    /// <param name="promptName">An optional exact prompt-range name filter.</param>
    /// <returns>A task containing the generated tree list.</returns>
    public async Task<PromptItemTreeList> GetPromptRangeTreeList(bool showPromptRangeNode, bool showTacticNode, string promptName = null)
    {
        PromptItemTreeList tree = new();

        var promptRangeService = _promptRangeService;

        //Legacy comment translated to English.

        var seh = new SenparcExpressionHelper<Models.DatabaseModel.PromptRange>();
        seh.ValueCompare.AndAlso(!promptName.IsNullOrEmpty(), z => z.RangeName == promptName);
        var where = seh.BuildWhereExpression();
        var allPromptRanges = await promptRangeService.GetFullListAsync(where, z => z.Id, OrderingType.Ascending);
        var promptRangeIds =allPromptRanges.Select(x => x.Id);
        
        var allPromptItems = await this.GetFullListAsync(z => true/*promptRangeIds.Contains(z.Id)*/, z => z.Id, OrderingType.Ascending);

        const string treeBranchMark = "┣";
        const string treeBranchArrowMark = "▽";
        const string treeBranchDarkArrowMark = "▼";
        const string treeBranchStr = treeBranchMark + "　";//Legacy comment translated to English.
        const string treeBranchArrowStr = treeBranchArrowMark + " ";//Legacy comment translated to English.
        const string treeBranchDarkArrowStr = treeBranchDarkArrowMark + " ";//Legacy comment translated to English.

        //Legacy comment translated to English.
        Func<int, bool, bool, string> GetPrefix = (level, isTacticPoint, showArrow) =>
        {
            var prefix = string.Concat(Enumerable.Repeat(treeBranchStr, level));

            if (!showArrow)
            {
                return prefix;
            }
            else
            {
                return prefix + (level == 1 && isTacticPoint ? treeBranchDarkArrowStr : treeBranchArrowStr);
            }
        };

        //Legacy comment translated to English.
        Func<decimal, string> GetScore = score => score < 0 ? "-" : score.ToString();


        foreach (var promptRange in allPromptRanges)
        {
            //Legacy comment translated to English.
            var rangeTree = await this.GenerateTacticTreeAsync(allPromptItems, promptRange.RangeName);

            //Console.WriteLine("rangeTree:" + rangeTree.Select(z => new { z.Name, z.Data.FullVersion }).ToJson(true));

            if (rangeTree.Count == 0)
            {
                continue;
            }

            List<string> addedTopNode = new List<string>();

            if (showPromptRangeNode)
            {
                //Legacy comment translated to English.
                tree.AddNode("PromptRange" + promptRange.Id, $"⊙ {promptRange.RangeName}（{promptRange.GetAvailableName()}）", promptRange.RangeName, -1, rangeTree.Count);
            }

            PromptItemVersion lastVersion = new PromptItemVersion("", "", -1);

            foreach (var treeNode in rangeTree)
            {
                //  the rangeTree and its children
                TraversePromptItem(treeNode);
            }

            //Legacy comment translated to English.
            void TraversePromptItem(TreeNode<PromptItem_GetIdAndNameResponse> treeNote)
            {
                var versionObj = treeNote.Data.PromptItemVersion;

                string lastTactic = string.Empty;

                //Legacy comment translated to English.
                if (showTacticNode && treeNote.Level == 1 && !addedTopNode.Contains(versionObj.Tactic))
                {
                    //Legacy comment translated to English.
                    var version = $"{versionObj.RangeName}-T{versionObj.Tactic}";
                    tree.AddNode(key: "PromptItemTactic" + treeNote.Data.PromptItemVersion.Tactic,
                                 text: GetPrefix(treeNote.Level, true, true) + version,
                                 value: version,
                                 level: treeNote.Level,
                                 subNodeCount: 0);

                    addedTopNode.Add(versionObj.Tactic);
                }

                //Legacy comment translated to English.
                var nickName = treeNote.NickName.IsNullOrEmpty() ? "" : $"{treeNote.NickName}，";
                var text = GetPrefix(treeNote.Level, false, treeNote.Children.Count > 0) + /*$"[{treeNote.Data.Id}]" +*/ $"{treeNote.Name} ({nickName}Avg:{GetScore(treeNote.Data.EvalAvgScore)}，Max:{GetScore(treeNote.Data.EvalMaxScore)})：{treeNote.Data.PromptContent.SubString(0, 30)}";

                tree.AddNode("PromptItem" + treeNote.Data.Id, text, treeNote.Data.FullVersion, treeNote.Level, treeNote.Children.Count);
                //Legacy comment translated to English.

                foreach (var child in treeNote.Children)
                {
                    TraversePromptItem(child);
                }

                lastTactic = versionObj.Tactic;
            }
        }

        return tree;
    }

    #endregion
}

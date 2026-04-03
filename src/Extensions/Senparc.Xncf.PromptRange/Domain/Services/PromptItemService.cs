using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
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

    public async Task<PromptItem> ForUnitTest(int id)
    {
        return await base.RepositoryBase.GetFirstOrDefaultObjectAsync(z => z.Id == id);
    }

    /// <summary>
    /// Newly added, during target practice
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<PromptItemDto> AddPromptItemAsync(PromptItem_AddRequest request)
    {
        #region validate request dto

        // IsNewTactic IsNewSubTactic cannot be True at the same time
        if (request.IsTopTactic && request.IsNewTactic && request.IsNewSubTactic)
        {
            throw new NcfExceptionBase("IsTopTactic IsNewTactic IsNewSubTactic不能同时为True");
        }

        // The default value is 2000
        request.MaxToken = request.MaxToken > 0 ? request.MaxToken : 2000;
        request.StopSequences = request.StopSequences == "" ? null : request.StopSequences;

        if (request.RangeId <= 0)
        {
            throw new NcfExceptionBase($"RangeId必须为正整数，当前是{request.RangeId}");
        }

        #endregion

        #region 获取靶场

        var promptRange = await _promptRangeService.GetAsync(request.RangeId);

        #endregion

        // //Update version number
        // var today = SystemTime.Now;
        // var todayStr = today.ToString("yyyy.MM.dd");

        #region 根据参数构造PromptItem

        PromptItem toSavePromptItem;
        if (request.Id == null)
        {
            //New PromptItem
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
            // If there is an id, first find the corresponding promptItem, then get the promptRange based on Item.RangeId, and then create a new target channel based on the parameters.
            // var basePrompt = await base.GetObjectAsync(p => p.Id == request.Id);
            var basePrompt = await this.GetAsync(request.Id.Value);
            // var promptRange = await _promptRangeService.GetObjectAsync(r => r.Id == basePrompt.RangeId);

            string rangeName = basePrompt.RangeName;

            if (request.IsTopTactic)
            {
                // The parent T of the target version number is an empty string
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
                // Copy the expected results by association
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewTactic)
            {
                // The parent T of the target version number should be the parent T of the current version
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
                // Copy the expected results by association
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewSubTactic)
            {
                // The parent T of the target version number should be the current version T
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
                // Copy the expected results by association
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewAiming) // Do not change branches
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
                // Copy the expected results by association
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else
            {
                SenparcTrace.SendCustomLog("AddPrompt", "指示符都是false, 没有新增");
                return basePrompt;
                // // burst
                // var promptResultService = _serviceProvider.GetService<PromptResultService>();
                // var resultDto = await promptResultService.SenparcGenerateResultAsync(basePrompt);
            }
        }

        #endregion

        // Before saving, verify whether the version number already exists to ensure the uniqueness of the version number.
        var existPromptItem = await base.GetObjectAsync(p => p.FullVersion == toSavePromptItem.FullVersion);
        if (existPromptItem != null)
        {
            throw new NcfExceptionBase("版号生成错误，请重新打靶");
        }

        await base.SaveObjectAsync(toSavePromptItem);

        return await this.TransEntityToDtoAsync(toSavePromptItem);
    }

    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(List<PromptItem> allPromptItems, PromptItem promptItem)
    {
        return await this.GenerateTacticTreeAsync(allPromptItems, promptItem.RangeName);
    }

    /// <summary>
    /// Score trend chart (based on time)
    /// TODO Change to display the trend chart of all promptItems with average scores in the shooting range
    /// </summary>
    /// <param name="promptItemId"></param>
    /// <returns></returns>
    public async Task<PromptItem_HistoryScoreResponse> GetHistoryScoreAsync(int promptItemId)
    {
        var versionHistoryList = new List<string>();
        var avgScoreHistoryList = new List<decimal>();
        var maxScoreHistoryList = new List<decimal>();

        var curItem = await this.GetAsync(promptItemId);

        // Get all scored items under the same target lane
        List<PromptItem> fullList = await this.GetFullListAsync(
            p => p.RangeName == curItem.RangeName && p.EvalAvgScore >= 0 && p.EvalMaxScore >= 0,
            p => p.Id,
            OrderingType.Ascending);

        // Construct return value
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

    public async Task<PromptItemDto> UpdateExpectedResultsAsync(int promptItemId, string expectedResults)
    {
        var promptItem = await this.GetObjectAsync(p => p.Id == promptItemId) ??
                         throw new Exception("未找到prompt");

        promptItem.UpdateExpectedResultsJson(expectedResults);

        await this.SaveObjectAsync(promptItem);

        return this.Mapper.Map<PromptItemDto>(promptItem);
    }

    public async Task<Statistic_TodayTacticResponse> GetLineChartDataAsync(int promptItemId, bool isAvg)
    {
        var promptItem = await this.GetObjectAsync(p => p.Id == promptItemId) ??
                         throw new Exception("未找到prompt");


        // Get all scored items under the same target lane
        List<PromptItemDto> promptItems = (await this.GetFullListAsync(
                p => p.RangeId == promptItem.RangeId && (isAvg ? p.EvalAvgScore >= 0 : p.EvalMaxScore >= 0),
                p => p.Id,
                OrderingType.Ascending)
            )
            .Select(p => this.Mapper.Map<PromptItemDto>(p))
            .ToList();

        // According to the first level of Tactic (the part before the first period number, such as "1", "11", and "1.1" are extracted as "1", "11", and "1" respectively)
        var itemGroupByT = promptItems.GroupBy(p => p.Tactic.Split('.')[0])
            .ToDictionary(p => p.Key, p => p.ToList());

        var resp = new Statistic_TodayTacticResponse(promptItem.RangeName, DateTime.Now);

        // [t1, version number, average score]
        // [t2, version number, average score]
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

    public async Task<PromptItemDto> GetAsync(int id)
    {
        var item = await this.GetObjectAsync(p => p.Id == id) ??
                   throw new NcfExceptionBase($"找不到{id}对应的promptItem");

        return await this.TransEntityToDtoAsync(item, needRange: true);
    }

    public async Task<PromptItemDto> DraftSwitch(int id, bool status)
    {
        var promptItem = await this.GetObjectAsync(p => p.Id == id) ??
                         throw new NcfExceptionBase($"找不到{id}对应的靶道");

        promptItem.DraftSwitch(status);

        await this.SaveObjectAsync(promptItem);

        return await this.TransEntityToDtoAsync(promptItem);
    }

    /// <summary>
    /// Get a certain version of PromptItem and model information, support:
    /// <para>Precise search, such as: 2024.01.06.3-T1-A2</para>
    /// <para>Target fuzzy search: Enter the shooting range and target information, such as: 2024.01.06.3-T1</para>
    /// <para>Fuzzy search of shooting range: enter only the shooting range number, such as: 2024.01.06.3</para>
    /// </summary>
    /// <param name="promptRangeVersion"></param>
    /// <param name="isAvg">When fuzzy search, whether to use the average highest score, if it is false, then directly take the highest score</param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<SenparcAI_GetByVersionResponse> GetWithVersionAsync(string promptRangeVersion, bool isAvg = true)
    {
        var promptItem = await GetBestPromptAsync(promptRangeVersion, isAvg);

        var dto = await this.TransEntityToDtoAsync(promptItem); // this.Mapper.Map<PromptItemDto>(item);

        //var aiModel = await _aiModelService.GetObjectAsync(model => model.Id == dto.ModelId) ??
        //              throw new NcfExceptionBase($"Cannot find the AIModel corresponding to {dto.ModelId}");

        //dto.AIModelDto = new AIModelDto(aiModel)
        //{
        //    ApiKey = aiModel.ApiKey,
        //    OrganizationId = aiModel.OrganizationId
        //};

        return new SenparcAI_GetByVersionResponse(_aiModelService.BuildSenparcAiSetting(dto.AIModelDto), dto);
    }


    /// <summary>
    /// Get a certain version of PromptItem and model information, support:
    /// <para>Precise search, such as: 2024.01.06.3-T1-A2</para>
    /// <para>Target fuzzy search: Enter the shooting range and target information, such as: 2024.01.06.3-T1</para>
    /// <para>Fuzzy search of shooting range: enter only the shooting range number, such as: 2024.01.06.3</para>
    /// </summary>
    /// <param name="promptRangeVersion"></param>
    /// <param name="isAvg">When fuzzy search, whether to use the average highest score, if it is false, then directly take the highest score</param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<PromptItem> GetBestPromptAsync(string promptRangeVersion, bool isAvg)
    {
        PromptItem promptItem;
        if (promptRangeVersion.Contains("-T") && promptRangeVersion.Contains("-A"))
        {
            //Accurately query the tested PromptItem, such as: 2024.01.06.3-T1-A2
            promptItem = await this.GetObjectAsync(p => p.FullVersion == promptRangeVersion) ??
                         throw new NcfExceptionBase($"找不到 {promptRangeVersion} 对应的 PromptItem");
        }
        else
        {
            //Fuzzy query, such as: 2024.01.06.3-T1, or 2024.01.06.3

            var searchTactic = promptRangeVersion.Contains("-T");

            var versionSet = promptRangeVersion.Split(new[] { "-T" }, StringSplitOptions.None);

            // validate rangeName
            var rangeName = versionSet[0];
            var promptRange = await _promptRangeService.GetObjectAsync(r => r.RangeName == rangeName)
                ?? throw new NcfExceptionBase($"找不到 {rangeName} 对应的靶场");

            var seh = new SenparcExpressionHelper<PromptItem>();
            seh.ValueCompare
                .AndAlso(true, z => z.RangeId == promptRange.Id/* z.RangeName == rangeName*/) //Positioning range
                .AndAlso(isAvg, z => z.EvalAvgScore >= 0) //average score
                .AndAlso(!isAvg, z => z.EvalMaxScore >= 0); //highest score

            if (searchTactic)
            {
                //Fuzzy search based on target lane
                var tactic = versionSet[1];
                seh.ValueCompare.AndAlso(true, z => z.Tactic == tactic);//target lane
            }
            else
            {
                //Fuzzy search by shooting range
                //No need to add any more conditions
            }

            //Generate the final query condition expression
            var where = seh.BuildWhereExpression();

            //Fuzzy search from a target lane
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


    private async Task<PromptItemDto> TransEntityToDtoAsync(PromptItem promptItem, bool needModel = true, bool needRange = true)
    {
        var promptItemDto = this.Mapper.Map<PromptItemDto>(promptItem);

        #region 补充AIModel信息

        if (needModel)
        {
            var aiModel = await _aiModelService.GetObjectAsync(model => model.Id == promptItem.ModelId);
            // ?? throw new NcfExceptionBase($"Cannot find the AIModel corresponding to {promptItem.ModelId}");
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

        #region 补充靶场信息

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
    /// Get model parameters according to configuration
    /// </summary>
    /// <param name="promptItem"></param>
    /// <returns></returns>
    public PromptConfigParameter GetPromptConfigParameterFromAiSetting(PromptItemDto promptItem)
    {
        //Define AI interface calling parameters and Token restrictions, etc.
        var promptParameter = new PromptConfigParameter()
        {
            MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 200,
            Temperature = promptItem.Temperature,
            TopP = promptItem.TopP,
            FrequencyPenalty = promptItem.FrequencyPenalty,
            PresencePenalty = promptItem.PresencePenalty,
            StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>(),
        };

        return promptParameter;
    }

    #region 生成 PromptItem 树

    /// <summary>
    /// Enter the shooting range name to build all version trees in the shooting range
    /// </summary>
    /// <param name="rangeName">Range name</param>
    /// <returns >Version tree</returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(List<PromptItem> allPromptitems, string rangeName)
    {
        // Get all PromptItems under the same target lane
        List<PromptItem> fullList = allPromptitems.Where(p => p.RangeName == rangeName).ToList();
        //Console.WriteLine("fulllist:" + fullList.OrderBy(z=>z.Id).Select(z => new { z.Id, z.RangeName, z.Tactic, z.Aiming, z.FullVersion,z.ParentTac }).ToJson(true));

        //Set top node (Tx)
        var rootNodeList = new List<TreeNode<PromptItem_GetIdAndNameResponse>>();

        //Find node method
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

            //Due to the temporal ordering characteristics of PromptItem, reverse search can find it faster
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

            //Due to the temporal ordering characteristics of PromptItem, reverse search can find it faster
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
            //Find superior nodes TODO: In order to improve efficiency, you can only search upwards

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

            //Create current new node information
            var newNode = new TreeNode<PromptItem_GetIdAndNameResponse>(promptitem.FullVersion, promptitem.NickName, new PromptItem_GetIdAndNameResponse(promptitem), -1);

            if (parentNode == null)
            {
                //top node
                newNode.Level = 1;
                rootNodeList.Add(newNode);
            }
            else
            {
                //child node
                newNode.Level = parentNode.Level + 1;
                parentNode.Children.Add(newNode);
            }
        }
        return rootNodeList;
    }


    /// <summary>
    /// Returns a PromptRange with a tree structure
    /// </summary>
    /// <returns></returns>
    public async Task<PromptItemTreeList> GetPromptRangeTreeList(bool showPromptRangeNode, bool showTacticNode, string promptName = null)
    {
        PromptItemTreeList tree = new();

        var promptRangeService = _promptRangeService;

        //Since the Tastic hierarchy develops downward according to time (ID) order, all nodes can be sorted from top to bottom as long as they are sorted according to ID.

        var seh = new SenparcExpressionHelper<Models.DatabaseModel.PromptRange>();
        seh.ValueCompare.AndAlso(!promptName.IsNullOrEmpty(), z => z.RangeName == promptName);
        var where = seh.BuildWhereExpression();
        var allPromptRanges = await promptRangeService.GetFullListAsync(where, z => z.Id, OrderingType.Ascending);
        var promptRangeIds =allPromptRanges.Select(x => x.Id);
        
        var allPromptItems = await this.GetFullListAsync(z => true/*promptRangeIds.Contains(z.Id)*/, z => z.Id, OrderingType.Ascending);

        const string treeBranchMark = "┣";
        const string treeBranchArrowMark = "▽";
        const string treeBranchDarkArrowMark = "▼";
        const string treeBranchStr = treeBranchMark + "　";//Add full-width spaces
        const string treeBranchArrowStr = treeBranchArrowMark + " ";//Add half-width space
        const string treeBranchDarkArrowStr = treeBranchDarkArrowMark + " ";//Add half-width space

        //Get columnar structure prefix
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

        //Read ratings
        Func<decimal, string> GetScore = score => score < 0 ? "-" : score.ToString();


        foreach (var promptRange in allPromptRanges)
        {
            //Get tree structure
            var rangeTree = await this.GenerateTacticTreeAsync(allPromptItems, promptRange.RangeName);

            //Console.WriteLine("rangeTree:" + rangeTree.Select(z => new { z.Name, z.Data.FullVersion }).ToJson(true));

            if (rangeTree.Count == 0)
            {
                continue;
            }

            List<string> addedTopNode = new List<string>();

            if (showPromptRangeNode)
            {
                //Starting a new PromptRange, inserting the overall guidance of this Prompt TODO: Determine whether additional descriptive nodes need to be added
                tree.AddNode("PromptRange" + promptRange.Id, $"⊙ {promptRange.RangeName}（{promptRange.GetAvailableName()}）", promptRange.RangeName, -1, rangeTree.Count);
            }

            PromptItemVersion lastVersion = new PromptItemVersion("", "", -1);

            foreach (var treeNode in rangeTree)
            {
                //  the rangeTree and its children
                TraversePromptItem(treeNode);
            }

            // Iteratively insert child nodes
            void TraversePromptItem(TreeNode<PromptItem_GetIdAndNameResponse> treeNote)
            {
                var versionObj = treeNote.Data.PromptItemVersion;

                string lastTactic = string.Empty;

                //Show Tactic virtual nodes
                if (showTacticNode && treeNote.Level == 1 && !addedTopNode.Contains(versionObj.Tactic))
                {
                    //Top level, such as: T1, plus drop-down mark
                    var version = $"{versionObj.RangeName}-T{versionObj.Tactic}";
                    tree.AddNode(key: "PromptItemTactic" + treeNote.Data.PromptItemVersion.Tactic,
                                 text: GetPrefix(treeNote.Level, true, true) + version,
                                 value: version,
                                 level: treeNote.Level,
                                 subNodeCount: 0);

                    addedTopNode.Add(versionObj.Tactic);
                }

                //Create current node
                var nickName = treeNote.NickName.IsNullOrEmpty() ? "" : $"{treeNote.NickName}，";
                var text = GetPrefix(treeNote.Level, false, treeNote.Children.Count > 0) + /*$"[{treeNote.Data.Id}]" +*/ $"{treeNote.Name} ({nickName}Avg:{GetScore(treeNote.Data.EvalAvgScore)}，Max:{GetScore(treeNote.Data.EvalMaxScore)})：{treeNote.Data.PromptContent.SubString(0, 30)}";

                tree.AddNode("PromptItem" + treeNote.Data.Id, text, treeNote.Data.FullVersion, treeNote.Level, treeNote.Children.Count);
                //Determine whether there are subordinates

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
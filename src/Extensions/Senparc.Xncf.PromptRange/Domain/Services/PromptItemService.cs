using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Azure;
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
    /// 新增， 打靶时
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<PromptItemDto> AddPromptItemAsync(PromptItem_AddRequest request)
    {
        #region validate request dto

        // IsNewTactic IsNewSubTactic不能同时为True
        if (request.IsTopTactic && request.IsNewTactic && request.IsNewSubTactic)
        {
            throw new NcfExceptionBase("IsTopTactic IsNewTactic IsNewSubTactic不能同时为True");
        }

        // 默认值为2000
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

        // // 更新版本号
        // var today = SystemTime.Now;
        // var todayStr = today.ToString("yyyy.MM.dd");

        #region 根据参数构造PromptItem

        PromptItem toSavePromptItem;
        if (request.Id == null)
        {
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
            // 如果有id，就先找到对应的promptItem, 再根据Item.RangeId获取promptRange，再根据参数新建一个靶道
            // var basePrompt = await base.GetObjectAsync(p => p.Id == request.Id);
            var basePrompt = await this.GetAsync(request.Id.Value);
            // var promptRange = await _promptRangeService.GetObjectAsync(r => r.Id == basePrompt.RangeId);

            string rangeName = basePrompt.RangeName;

            if (request.IsTopTactic)
            {
                // 目标版号的父 T 是空串
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
                // 关联复制预期结果过来
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewTactic)
            {
                // 目标版号的父 T 应该是当前版本的父 T
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
                // 关联复制预期结果过来
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewSubTactic)
            {
                // 目标版号的父 T 应该是当前版本的 T
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
                // 关联复制预期结果过来
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else if (request.IsNewAiming) // 不改变分支
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
                // 关联复制预期结果过来
                toSavePromptItem.UpdateExpectedResultsJson(basePrompt.ExpectedResultsJson, false);
            }
            else
            {
                SenparcTrace.SendCustomLog("AddPrompt", "指示符都是false, 没有新增");
                return basePrompt;
                // // 连发
                // var promptResultService = _serviceProvider.GetService<PromptResultService>();
                // var resultDto = await promptResultService.SenparcGenerateResultAsync(basePrompt);
            }
        }

        #endregion

        // 保存之前验证一下版号是否已经存在，确保版号唯一性
        var existPromptItem = await base.GetObjectAsync(p => p.FullVersion == toSavePromptItem.FullVersion);
        if (existPromptItem != null)
        {
            throw new NcfExceptionBase("版号生成错误，请重新打靶");
        }

        await base.SaveObjectAsync(toSavePromptItem);

        return await this.TransEntityToDtoAsync(toSavePromptItem);
    }


    // /// <summary>
    // /// 输入一个版本号，构建子版本树，包含自己，父版本，递归直到root
    // /// 即从该节点到root节点的最短路径
    // /// </summary>
    // /// <param name="curVersion">当前版本号</param>
    // /// <returns>版本树</returns>
    // /// <exception cref="NcfExceptionBase"></exception>
    // public async Task<TreeNode<PromptItem>> GenerateVersionTree(string curVersion)
    // {
    //     #region 找到对应的promptItem
    //
    //     var promptItem = await this.GetObjectAsync(p => p.FullVersion == curVersion);
    //     if (promptItem == null)
    //     {
    //         throw new NcfExceptionBase("找不到对应的promptItem");
    //     }
    //
    //     #endregion
    //
    //     return await this.GenerateVersionTree(promptItem);
    // }

    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(PromptItem promptItem)
    {
        return await this.GenerateTacticTreeAsync(promptItem.RangeName);
        // // 获取同一个靶道下的所有
        // List<PromptItem> fullList = await this.GetFullListAsync(p => p.RangeName == promptItem.RangeName);
        //
        // // 根据 FullVersion, 将list转为Dictionary，key为FullVersion
        // var itemMapByVersion = fullList.ToDictionary(p => p.FullVersion, p => p);
        //
        // // 根据 ParentTac, 将list转为Dictionary<string,List<PromptItem>>
        // var itemGroupByParentTac = fullList.GroupBy(p => p.ParentTac)
        //     .ToDictionary(p => p.Key, p => p.ToList());
        //
        // PromptItem rootItem = itemMapByVersion[$"{promptItem.RangeName}-T1-A1"];
        // TreeNode<PromptItem> rootNode = new TreeNode<PromptItem>(rootItem.FullVersion, rootItem);
        //
        // // 递归构建树
        // this.BuildVersionTreeHelper(rootNode, itemMapByVersion, itemGroupByParentTac);
        //
        // return rootNode;
    }

    //private void BuildVersionTreeHelper(TreeNode<PromptItem_GetIdAndNameResponse> rootNode,
    //    Dictionary<string, PromptItem> itemMapByVersion,
    //    Dictionary<string, List<PromptItem>> itemGroupByParentTac)
    //{
    //    var root = itemMapByVersion[rootNode.Name];
    //    if (!itemGroupByParentTac.ContainsKey(root.Tactic))
    //    {
    //        return;
    //    }

    //    var promptItems = itemGroupByParentTac[root.Tactic];
    //    foreach (var childItem in promptItems)
    //    {
    //        var childNode = new TreeNode<PromptItem_GetIdAndNameResponse>(childItem.FullVersion, childItem.NickName, new PromptItem_GetIdAndNameResponse(childItem));
    //        this.BuildVersionTreeHelper(childNode, itemMapByVersion, itemGroupByParentTac);
    //        rootNode.Children.Add(childNode);
    //    }
    //}


    /// <summary>
    /// 分数趋势图（依据时间）
    /// TODO 改为显示靶场下所有有平均分的promptItem的趋势图
    /// </summary>
    /// <param name="promptItemId"></param>
    /// <returns></returns>
    public async Task<PromptItem_HistoryScoreResponse> GetHistoryScoreAsync(int promptItemId)
    {
        var versionHistoryList = new List<string>();
        var avgScoreHistoryList = new List<decimal>();
        var maxScoreHistoryList = new List<decimal>();

        var curItem = await this.GetAsync(promptItemId);

        // 获取同一个靶道下的所有打过分的item
        List<PromptItem> fullList = await this.GetFullListAsync(
            p => p.RangeName == curItem.RangeName && p.EvalAvgScore >= 0 && p.EvalMaxScore >= 0,
            p => p.Id,
            OrderingType.Ascending);

        // 构造返回值
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


        // 获取同一个靶道下的所有打过分的item
        List<PromptItemDto> promptItems = (await this.GetFullListAsync(
                p => p.RangeId == promptItem.RangeId && (isAvg ? p.EvalAvgScore >= 0 : p.EvalMaxScore >= 0),
                p => p.Id,
                OrderingType.Ascending)
            )
            .Select(p => this.Mapper.Map<PromptItemDto>(p))
            .ToList();

        // 根据 Tactic 的第一位, 将 list 转为 Dictionary<string,List<PromptItem>>
        var itemGroupByT = promptItems.GroupBy(p => p.Tactic.Substring(0, 1))
            .ToDictionary(p => p.Key, p => p.ToList());

        var resp = new Statistic_TodayTacticResponse(promptItem.RangeName, DateTime.Now);

        // [t1, 版号, 平均分]
        // [t2, 版号, 平均分]
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
    /// 获取某个版本的 PromptItem 和模型信息，支持：
    /// <para>精准搜索，如：2024.01.06.3-T1-A2</para>
    /// <para>靶道模糊搜索：输入到靶场和靶道信息，如：2024.01.06.3-T1</para>
    /// <para>靶场模糊搜索：只输入靶场编号，如：2024.01.06.3</para>
    /// </summary>
    /// <param name="promptRangeVersion"></param>
    /// <param name="isAvg">当模糊搜索时，是否采用平均分最高分，如果为 false，则直接取最高分</param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<SenparcAI_GetByVersionResponse> GetWithVersionAsync(string promptRangeVersion, bool isAvg = true)
    {
        var promptItem = await GetBestPromptAsync(promptRangeVersion, isAvg);

        var dto = await this.TransEntityToDtoAsync(promptItem); // this.Mapper.Map<PromptItemDto>(item);

        //var aiModel = await _aiModelService.GetObjectAsync(model => model.Id == dto.ModelId) ??
        //              throw new NcfExceptionBase($"找不到{dto.ModelId}对应的AIModel");

        //dto.AIModelDto = new AIModelDto(aiModel)
        //{
        //    ApiKey = aiModel.ApiKey,
        //    OrganizationId = aiModel.OrganizationId
        //};

        return new SenparcAI_GetByVersionResponse(_aiModelService.BuildSenparcAiSetting(dto.AIModelDto), dto);
    }


    /// <summary>
    /// 获取某个版本的 PromptItem 和模型信息，支持：
    /// <para>精准搜索，如：2024.01.06.3-T1-A2</para>
    /// <para>靶道模糊搜索：输入到靶场和靶道信息，如：2024.01.06.3-T1</para>
    /// <para>靶场模糊搜索：只输入靶场编号，如：2024.01.06.3</para>
    /// </summary>
    /// <param name="promptRangeVersion"></param>
    /// <param name="isAvg">当模糊搜索时，是否采用平均分最高分，如果为 false，则直接取最高分</param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<PromptItem> GetBestPromptAsync(string promptRangeVersion, bool isAvg)
    {
        PromptItem promptItem;
        if (promptRangeVersion.Contains("-T") && promptRangeVersion.Contains("-A"))
        {
            //精准查询经过测试的 PromptItem，如：2024.01.06.3-T1-A2
            promptItem = await this.GetObjectAsync(p => p.FullVersion == promptRangeVersion) ??
                         throw new NcfExceptionBase($"找不到 {promptRangeVersion} 对应的 PromptItem");
        }
        else
        {
            //模糊查询，如：2024.01.06.3-T1，或者 2024.01.06.3

            var searchTactic = promptRangeVersion.Contains("-T");

            var versionSet = promptRangeVersion.Split(new[] { "-T" }, StringSplitOptions.None);

            // validate rangeName
            var rangeName = versionSet[0];
            var promptRange = await _promptRangeService.GetObjectAsync(r => r.RangeName == rangeName)
                ?? throw new NcfExceptionBase($"找不到 {rangeName} 对应的靶场");

            var seh = new SenparcExpressionHelper<PromptItem>();
            seh.ValueCompare
                .AndAlso(true, z => z.RangeId == promptRange.Id/* z.RangeName == rangeName*/) //定位靶场
                .AndAlso(isAvg, z => z.EvalAvgScore >= 0) //平均分
                .AndAlso(!isAvg, z => z.EvalMaxScore >= 0); //最高分

            if (searchTactic)
            {
                //按照靶道进行模糊搜索
                var tactic = versionSet[1];
                seh.ValueCompare.AndAlso(true, z => z.Tactic == tactic);//定位靶道
            }
            else
            {
                //按照靶场进行模糊搜索
                //不需要再增加条件
            }

            //生成最终的查询条件表达式
            var where = seh.BuildWhereExpression();

            //从某个靶道进行模糊搜索
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
            // ?? throw new NcfExceptionBase($"找不到{promptItem.ModelId}对应的AIModel");
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
    /// 根据配置获取模型参数
    /// </summary>
    /// <param name="promptItem"></param>
    /// <returns></returns>
    public PromptConfigParameter GetPromptConfigParameterFromAiSetting(PromptItemDto promptItem)
    {
        //定义 AI 接口调用参数和 Token 限制等
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

    /// <summary>
    /// 输入靶场名，构建该靶场内所有的版本树
    /// </summary>
    /// <param name="rangeName">靶场名</param>
    /// <returns >版本树</returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(string rangeName)
    {
        // 获取同一个靶道下的所有的 PromptItem
        List<PromptItem> fullList = await this.GetFullListAsync(p => p.RangeName == rangeName, z => z.Id, OrderingType.Ascending);
        //Console.WriteLine("fulllist:" + fullList.OrderBy(z=>z.Id).Select(z => new { z.Id, z.RangeName, z.Tactic, z.Aiming, z.FullVersion,z.ParentTac }).ToJson(true));

        //设置顶部节点（Tx）
        var rootNodeList = new List<TreeNode<PromptItem_GetIdAndNameResponse>>();

        TreeNode<PromptItem_GetIdAndNameResponse> findNode(TreeNode<PromptItem_GetIdAndNameResponse> parentNode, string promptRange, string tascic)
        {
            if (parentNode == null)
            {
                return null;
            }

            if (parentNode.Data.PromptItemVersion.RangeName == rangeName && parentNode.Data.PromptItemVersion.Tactic == tascic)
            {
                return parentNode;
            }

            foreach (var item in parentNode.Children)
            {
                var result = findNode(item, promptRange, tascic);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        foreach (var promptitem in fullList)
        {
            //寻找上级节点    TODO：为了提高效率，可以只向上查找

            TreeNode<PromptItem_GetIdAndNameResponse> parentNode = null;
            foreach (var item in rootNodeList)
            {
                parentNode = findNode(item, promptitem.RangeName, promptitem.ParentTac);
                if (parentNode!=null)
                {
                    break;
                }
            }

            //创造当前新节点信息
            var newNode = new TreeNode<PromptItem_GetIdAndNameResponse>(promptitem.FullVersion, promptitem.NickName, new PromptItem_GetIdAndNameResponse(promptitem), -1);

            if (parentNode == null)
            {
                //顶部节点
                newNode.Level = 1;
                rootNodeList.Add(newNode);
            }
            else
            {
                //子节点
                newNode.Level = parentNode.Level + 1;
                parentNode.Children.Add(newNode);
            }
        }
        return rootNodeList;
    }


    /// <summary>
    /// 返回带树形结构的 PromptRange
    /// </summary>
    /// <returns></returns>
    public async Task<PromptItemTreeList> GetPromptRangeTreeList()
    {
        PromptItemTreeList tree = new();

        var promptRangeService = _promptRangeService;

        //由于 Tastic 层次是根据时间（ID）顺序向下发展，所以只要根据 ID 排序，即可实现所有节点自顶向下的排列顺序
        var allPromptRanges = await promptRangeService.GetFullListAsync(z => true, z => z.Id, OrderingType.Ascending);

        const string treeBranchMark = "┣";
        const string treeBranchArrowMark = "▽";
        const string treeBranchDarkArrowMark = "▼";
        const string treeBranchStr = treeBranchMark + " ";
        const string treeBranchArrowStr = treeBranchArrowMark + " ";
        const string treeBranchDarkArrowStr = treeBranchDarkArrowMark + " ";

        //获取柱状结构前缀
        Func<int, bool, string> GetPrefix = (level, showArrow) =>
        {
            var prefix = string.Concat(Enumerable.Repeat(treeBranchStr, level));

            if (!showArrow)
            {
                return prefix;
            }
            else
            {
                return prefix + (level == 1 ? treeBranchDarkArrowStr : treeBranchArrowStr);
            }
        };

        //读取评分
        Func<decimal, string> GetScore = score => score < 0 ? "-" : score.ToString();
        foreach (var promptRange in allPromptRanges)
        {
            //获取树状结构
            var rangeTree = await this.GenerateTacticTreeAsync(promptRange.RangeName);

            //Console.WriteLine("rangeTree:" + rangeTree.Select(z => new { z.Name, z.Data.FullVersion }).ToJson(true));

            if (rangeTree.Count == 0)
            {
                continue;
            }

            List<string> addedTopNode = new List<string>();

            //正在开始一个新的 PromptRange，插入这个 Prompt的整体引导 TODO：判断是否需要添加额外描述性节点
            tree.AddNode("PromptRange" + promptRange.Id, $"{promptRange.RangeName}（{promptRange.GetAvailableName()}）", promptRange.RangeName, -1, rangeTree.Count);

            PromptItemVersion lastVersion = new PromptItemVersion("", "", -1);

            foreach (var treeNode in rangeTree)
            {
                //  the rangeTree and its children
                TraversePromptItem(treeNode);
            }


            void TraversePromptItem(TreeNode<PromptItem_GetIdAndNameResponse> treeNote)
            {
                var versionObj = treeNote.Data.PromptItemVersion;

                string lastTactic = string.Empty;

                if (treeNote.Level == 1 && !addedTopNode.Contains(versionObj.Tactic))
                {
                    //顶层，如：T1，加上下拉标记
                    var version = $"{versionObj.RangeName}-T{versionObj.Tactic}";
                    tree.AddNode(key: "PromptItemTactic" + treeNote.Data.PromptItemVersion.Tactic,
                                 text: GetPrefix(treeNote.Level, true) + version,
                                 value: version,
                                 level: treeNote.Level,
                                 subNodeCount: 0);

                    addedTopNode.Add(versionObj.Tactic);
                }

                //创建当前节点
                var nickName = treeNote.NickName.IsNullOrEmpty() ? "" : $"{treeNote.NickName}，";
                var text = GetPrefix(treeNote.Level, treeNote.Children.Count > 0) + /*$"[{treeNote.Data.Id}]" +*/ $" {treeNote.Name}({nickName}Avg:{GetScore(treeNote.Data.EvalAvgScore)}，Max:{GetScore(treeNote.Data.EvalMaxScore)})：{treeNote.Data.PromptContent.SubString(0, 30)}";

                tree.AddNode("PromptItem" + treeNote.Data.Id, text, treeNote.Data.FullVersion, treeNote.Level, treeNote.Children.Count);
                //判断是否还有下级

                foreach (var child in treeNote.Children)
                {
                    TraversePromptItem(child);
                }

                lastTactic = versionObj.Tactic;
            }
        }

        return tree;
    }
}
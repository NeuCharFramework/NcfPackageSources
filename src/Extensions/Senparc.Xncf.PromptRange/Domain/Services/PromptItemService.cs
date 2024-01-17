using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Utility;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public class PromptItemService : ServiceBase<PromptItem>
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


    /// <summary>
    /// 输入靶场名，构建该靶场内所有的版本树
    /// </summary>
    /// <param name="rangeName">靶场名</param>
    /// <returns >版本树</returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync(string rangeName)
    {
        // 获取同一个靶道下的所有
        List<PromptItem> fullList = await this.GetFullListAsync(p => p.RangeName == rangeName);

        // 根据 FullVersion, 将list转为Dictionary，key为FullVersion
        var itemMapByVersion = fullList.ToDictionary(p => p.FullVersion, p => p);

        // 根据 ParentTac, 将list转为Dictionary<string,List<PromptItem>>
        var itemGroupByParentTac = fullList.GroupBy(p => p.ParentTac)
            .ToDictionary(p => p.Key, p => p.ToList());

        // 先处理第一级
        var rootNodeList = new List<TreeNode<PromptItem_GetIdAndNameResponse>>();

        List<PromptItem> topTierItemList = itemGroupByParentTac[""];
        foreach (var rootItem in topTierItemList)
        {
            // PromptItem rootItem = itemMapByVersion[$"{rangeName}-T1-A1"];
            var rootNode = new TreeNode<PromptItem_GetIdAndNameResponse>(rootItem.FullVersion, new PromptItem_GetIdAndNameResponse(rootItem));

            // 递归构建树
            this.BuildVersionTreeHelper(rootNode, itemMapByVersion, itemGroupByParentTac);

            rootNodeList.Add(rootNode);
        }

        return rootNodeList;
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

    public async Task<List<TreeNode<PromptItem_GetIdAndNameResponse>>> GenerateTacticTreeAsync([NotNull] PromptItem promptItem)
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

    private void BuildVersionTreeHelper(TreeNode<PromptItem_GetIdAndNameResponse> rootNode,
        Dictionary<string, PromptItem> itemMapByVersion,
        Dictionary<string, List<PromptItem>> itemGroupByParentTac)
    {
        var root = itemMapByVersion[rootNode.Name];
        if (!itemGroupByParentTac.ContainsKey(root.Tactic))
        {
            return;
        }

        var promptItems = itemGroupByParentTac[root.Tactic];
        foreach (var childItem in promptItems)
        {
            var childNode = new TreeNode<PromptItem_GetIdAndNameResponse>(childItem.FullVersion, new PromptItem_GetIdAndNameResponse(childItem));
            this.BuildVersionTreeHelper(childNode, itemMapByVersion, itemGroupByParentTac);
            rootNode.Children.Add(childNode);
        }
    }


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
    /// <param name="fullVersion"></param>
    /// <param name="isAvg">当模糊搜索时，是否采用平均分最高分，如果为 false，则直接取最高分</param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<SenparcAI_GetByVersionResponse> GetWithVersionAsync(string fullVersion, bool isAvg = true)
    {
        var promptItem = await GetBestPromptAsync(fullVersion, isAvg);

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
    /// <param name="fullVersion"></param>
    /// <param name="isAvg">当模糊搜索时，是否采用平均分最高分，如果为 false，则直接取最高分</param>
    /// <returns></returns>
    /// <exception cref="NcfExceptionBase"></exception>
    public async Task<PromptItem> GetBestPromptAsync(string fullVersion, bool isAvg)
    {
        PromptItem promptItem;
        if (fullVersion.Contains("-T") && fullVersion.Contains("-A"))
        {
            //精准查询
            promptItem = await this.GetObjectAsync(p => p.FullVersion == fullVersion) ??
                         throw new NcfExceptionBase($"找不到 {fullVersion} 对应的 PromptItem");
        }
        else
        {
            //模糊查询

            var versionSet = fullVersion.Split(new[] { "-T" }, StringSplitOptions.None);

            // validate rangeName
            var rangeName = versionSet[0];
            var promptRange = await _promptRangeService.GetObjectAsync(r => r.RangeName == rangeName) ??
                              throw new NcfExceptionBase($"找不到 {rangeName} 对应的靶场");

            var seh = new SenparcExpressionHelper<PromptItem>();
            seh.ValueCompare
                .AndAlso(true, z => z.RangeName == promptRange.RangeName) //靶场编号
                .AndAlso(isAvg, z => z.EvalAvgScore >= 0) //平均分
                .AndAlso(!isAvg, z => z.EvalMaxScore >= 0); //最高分

            if (fullVersion.Contains("-T"))
            {
                //按照靶道进行模糊搜索
                var tactic = versionSet[1];
                seh.ValueCompare.AndAlso(true, z => z.Tactic == tactic);
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
            throw new Exception("找不到匹配条件的 PromptItem");
        }

        return promptItem;
    }


    [ItemNotNull]
    private async Task<PromptItemDto> TransEntityToDtoAsync([NotNull] PromptItem promptItem, bool needModel = true, bool needRange = true)
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

    public async Task<string> ExportPluginsAsync(string fullVersion)
    {
        var item = await this.GetObjectAsync(p => p.FullVersion == fullVersion) ??
                   throw new NcfExceptionBase($"未找到{fullVersion}对应的提示词靶道");
        var rangePath = await this.ExportPluginWithItemAsync(item);

        return rangePath;
    }

    public async Task<string> ExportPluginsAsync([NotNull] IEnumerable<int> rangeIds, [CanBeNull] List<int> ids)
    {
        var rangeFilePaths = rangeIds.Select(rangeId =>
        {
            var pluginFilePath = this.ExportPluginsAsync(rangeId, ids);
            return pluginFilePath.Result;
        });

        // 根据 rangeFilePaths， 找出他们公共父文件夹的路径
        var commonParentPath = Path.GetDirectoryName(
            this.FindCommonParentPath(rangeFilePaths));

        return commonParentPath;
    }

    private string FindCommonParentPath(IEnumerable<string> paths)
    {
        var splitPaths = paths.Select(p => p.Split(Path.DirectorySeparatorChar)).ToList();
        var commonPath = new List<string>();
        for (int i = 0; i < splitPaths.Min(sp => sp.Length); i++)
        {
            var dir = splitPaths[0][i];
            if (splitPaths.All(sp => sp[i] == dir))
            {
                commonPath.Add(dir);
            }
            else
            {
                break;
            }
        }

        return string.Join(Path.DirectorySeparatorChar.ToString(), commonPath);
    }

    /// <summary>
    /// 根据靶场 ID, 导出该靶场下所有的靶道，返回文件夹路径
    /// </summary>
    /// <param name="rangeId"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<string> ExportPluginsAsync(int rangeId, [CanBeNull] List<int> ids)
    {
        // 根据靶场名，获取靶场
        var promptRange = await _promptRangeService.GetAsync(rangeId);

        // 获取输出的靶场的文件夹路径
        var rangePath = await this.GetRangePathAsync(promptRange);

        // 根据靶场名，获取靶道
        var promptItemList = await this.GetFullListAsync(
            p => p.RangeName == promptRange.RangeName
                 && (ids == null || ids.Contains(p.Id))
        );

        // //用版号作为key, 映射字典
        // var itemMapByVersion = promptItemList.ToDictionary(p => p.FullVersion, p => p);

        // // 提取出 T 的第一位，并分组
        // Dictionary<string, List<PromptItem>> itemGroupByT = promptItemList.GroupBy(p => p.Tactic.Substring(0, 1))
        //     .ToDictionary(p => p.Key, p => p.ToList());

        // 每个靶道都需要导出
        foreach (var item in promptItemList)
        {
            // // 找出最佳item
            // var bestItem = itemList.MaxBy(p => isAvg ? p.EvalAvgScore : p.EvalMaxScore);

            await ExportPluginWithItemAsync(item, rangePath);
        }

        return rangePath;
    }

    /// <summary>
    /// 导出指定的单个靶道，返回文件夹路径
    /// </summary>
    /// <param name="item"></param>
    /// <param name="rangePath"></param>
    /// <returns></returns>
    public async Task<string> ExportPluginWithItemAsync(PromptItem item, string rangePath = null)
    {
        var range = await _promptRangeService.GetAsync(item.RangeId);

        rangePath ??= await this.GetRangePathAsync(range);

        #region 根据模板构造 Root 对象

        var data = new Root()
        {
            Schema = 1,
            Description = "Generated by Senparc.Xncf.PromptRange",
            ExecutionSettings = new ExecutionSettings()
            {
                Default = new Default()
                {
                    MaxTokens = item.MaxToken,
                    Temperature = item.Temperature,
                    TopP = item.TopP,
                    PresencePenalty = item.PresencePenalty,
                    FrequencyPenalty = item.FrequencyPenalty,
                    StopSequences = (item.StopSequences ?? "[]").GetObject<List<string>>()
                }
            }
        };

        #endregion

        //  当前 plugin 文件夹目录，靶道名/别名
        var curPluginPath = Path.Combine(rangePath, item.NickName ?? item.FullVersion);
        if (!Directory.Exists(curPluginPath))
        {
            Directory.CreateDirectory(curPluginPath);
        }
        else
        {
            // 如果别名已经存在，就增加一个尾缀
            curPluginPath += $"_{DateTime.Now:yyyyMMddHHmmss}";
            Directory.CreateDirectory(curPluginPath);
        }

        // 完整的JSON文件路径
        // string jsonFullPath = Path.Combine(curPluginPath, "config.json");

        await using (var jsonFs = new FileStream(
                         Path.Combine(curPluginPath, "config.json"),
                         FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            jsonFs.Seek(0, SeekOrigin.Begin);
            jsonFs.SetLength(0); // 清空文件内容
            await using (var jsonSw = new StreamWriter(jsonFs, Encoding.UTF8))
            {
                // 写入并且保持格式
                await jsonSw.WriteLineAsync(JsonConvert.SerializeObject(data, Formatting.Indented));
            }
        }

        // 同理，构造 skprompt.txt 文件，内容为content
        await using (var txtFs = new FileStream(
                         Path.Combine(curPluginPath, "skprompt.txt"),
                         FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            txtFs.Seek(0, SeekOrigin.Begin);
            txtFs.SetLength(0); // 清空文件内容
            await using (var jsonSw = new StreamWriter(txtFs, Encoding.UTF8))
            {
                await jsonSw.WriteLineAsync(item.Content);
            }
        }

        return rangePath;
    }

    /// <summary>
    /// 根据靶场，生成文件夹，并返回文件夹路径
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    private async Task<string> GetRangePathAsync(PromptRangeDto range)
    {
        #region 根据靶场别名，生成文件夹

        // 有别名就用别名，没有就用靶场名

        // 先获取根目录
        var curDir = Directory.GetCurrentDirectory();
        await Console.Out.WriteLineAsync(curDir);
        var filePathPrefix = Path.Combine(curDir, "App_Data", "Files");
        // 生成文件夹
        var rangePath = Path.Combine(filePathPrefix, "ExportedPlugins", $"{range.Alias ?? range.RangeName}_{range.RangeName}");
        if (!Directory.Exists(rangePath))
        {
            Directory.CreateDirectory(rangePath);
        }

        #endregion

        return rangePath;
    }

    #region Inner Class

    class ExecutionSettings
    {
        [JsonProperty] public Default Default { get; set; }
    }

    class Default
    {
        public int MaxTokens { get; set; }
        public float Temperature { get; set; }
        public float TopP { get; set; }
        public float PresencePenalty { get; set; }
        public float FrequencyPenalty { get; set; }
        public List<string> StopSequences { get; set; }
    }

    class Root
    {
        public int Schema { get; set; }
        public string Description { get; set; }
        public ExecutionSettings ExecutionSettings { get; set; }
    }

    #endregion

    public async Task UploadPluginsAsync(IFormFile uploadedFile)
    {
        #region 验证文件

        if (uploadedFile == null || uploadedFile.Length == 0)
            throw new NcfExceptionBase("文件未找到");
        // 限制文件上传的大小为 50M
        if (uploadedFile.Length > 1024 * 1024 * 50)
        {
            throw new NcfExceptionBase("文件大小超过限制（50 M）");
        }

        if (!uploadedFile.FileName.EndsWith(".zip"))
        {
            throw new NcfExceptionBase("文件格式错误");
        }

        #endregion

        #region 保存文件

        var toSaveDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Files", "toImportFileTemp");
        if (!Directory.Exists(toSaveDir))
        {
            Directory.CreateDirectory(toSaveDir);
        }

        // 文件保存路径
        var zipFilePath = Path.Combine(toSaveDir, uploadedFile.FileName);

        using (var stream = new FileStream(zipFilePath, FileMode.Create))
        {
            await uploadedFile.CopyToAsync(stream);
        }

        #endregion

        // 先创建靶场
        var rangeAlias = uploadedFile.FileName.Split(".")[0];
        var promptRange = await _promptRangeService.AddAsync(rangeAlias);

        // 读取 zip 文件
        using var zip = ZipFile.OpenRead(zipFilePath);

        // #region 可以选择先解压

        // zip.ExtractToDirectory(Path.Combine(toSaveDir, zipFile.FileName.Split(".")[0]), true);

        // 解压文件
        // var unzippedFilePath = Path.Combine(toSaveDir, zipFile.FileName.Split(".")[0], "");
        // if (!Directory.Exists(unzippedFilePath))
        // {
        //     Directory.CreateDirectory(unzippedFilePath);
        // }

        // ZipFile.ExtractToDirectory(toSaveFilePath, unzippedFilePath, Encoding.UTF8, true);
        // ZipFile.ExtractToDirectory(zipFile.OpenReadStream(), unzippedFilePath, Encoding.UTF8, true);

        // #endregion

        // 开始读取
        Dictionary<string, PromptItem> zipIdxDict = new();
        int tacticCnt = 0;
        foreach (var curFile in zip.Entries)
        {
            // var curFilePath = Path.Combine(extractPath, entry.FullName);
            var curDirName = Path.GetDirectoryName(curFile.FullName)!;
            if (curDirName.Contains('/') || curDirName.Contains('\\'))
            {
                throw new NcfExceptionBase($"{curFile.FullName}文件格式错误");
            }

            if (curFile.Name == "") // 是目录
            {
                var promptItem = new PromptItem(promptRange, curDirName, ++tacticCnt);

                zipIdxDict[curDirName] = promptItem;
            }
            else
            {
                // var directoryName = curDirName!;
                // if (directoryName.Contains('/') || directoryName.Contains('\\'))
                // {
                //     throw new NcfExceptionBase($"{curFile.FullName}文件格式错误");
                // }

                // 从缓存中读取
                var promptItem = zipIdxDict[curDirName];

                // 根据不同文件名，更新不同的字段
                if (curFile.Name == "config.json") // 更新配置文件
                {
                    // 读取所有的文件为一个 string
                    await using Stream stream = curFile.Open();
                    using StreamReader reader = new StreamReader(stream);

                    string text = await reader.ReadToEndAsync();


                    var executionSettings = text.GetObject<Root>().ExecutionSettings!;

                    promptItem.UpdateModelParam(
                        topP: executionSettings.Default.TopP,
                        maxToken: executionSettings.Default.MaxTokens,
                        temperature: executionSettings.Default.Temperature,
                        presencePenalty: executionSettings.Default.PresencePenalty,
                        frequencyPenalty: executionSettings.Default.FrequencyPenalty,
                        stopSequences: executionSettings.Default.StopSequences.ToJson()
                    );
                }
                else if (curFile.Name == "skprompt.txt")
                {
                    // 读取所有的文件为一个 string
                    await using Stream stream = curFile.Open();
                    using StreamReader reader = new StreamReader(stream);

                    string skPrompt = await reader.ReadToEndAsync();

                    promptItem.UpdateContent(skPrompt);

                    // 提取 prompt 请求参数
                    var pattern = @"\{\{\$(.*?)\}\}";

                    // 没有参数
                    if (!Regex.IsMatch(skPrompt, pattern))
                    {
                        continue;
                    }

                    MatchCollection matches = Regex.Matches(skPrompt, pattern);
                    Dictionary<string, string> varDict = new();
                    foreach (Match match in matches)
                    {
                        string varKey = match.Groups[1].Value;
                        varDict[varKey] = "";
                    }

                    promptItem.UpdateVariablesJson(varDict.ToJson());
                }
                else
                {
                    continue;
                    throw new NcfExceptionBase($"{curFile.FullName}不符合上传要求");
                }
            }
        }

        // 保存
        await this.SaveObjectListAsync(zipIdxDict.Values.ToList());
    }
}
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptItemService : ServiceBase<PromptItem>
    {
        private readonly PromptResultService _promptResultService;

        public PromptItemService(
            PromptResultService promptResultService,
            IRepositoryBase<PromptItem> repo,
            IServiceProvider serviceProvider) : base(repo,
            serviceProvider)
        {
            _promptResultService = promptResultService;
        }

        /// <summary>
        /// 新增， 打靶时
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<PromptItem> AddPromptItemAsync(PromptItem_AddRequest request)
        {
            #region validate request dto

            // IsNewTactic IsNewSubTactic不能同时为True
            if (request.IsNewTactic && request.IsNewSubTactic)
            {
                throw new NcfExceptionBase("IsNewTactic IsNewSubTactic不能同时为True");
            }
            // var name = request.Content.SubString(0, 5);

            // 默认值为2000
            request.MaxToken = request.MaxToken > 0 ? request.MaxToken : 2000;
            request.StopSequences = request.StopSequences == "" ? null : request.StopSequences;

            #endregion


            // 更新版本号
            var today = SystemTime.Now;
            var todayStr = today.ToString("yyyy.MM.dd");

            #region 根据参数构造PromptItem

            PromptItem toSavePromptItem = null;
            if (request.Id == null)
            {
                // 如果没有id，就新建一个全新的Item

                List<PromptItem> todayPromptList = await base.GetFullListAsync(p => p.Name.StartsWith(todayStr));
                // var name = $"{todayStr}.{todayPromptList.Count + 1}";
                // var tactic = "1";
                // var aiming = 1;


                toSavePromptItem = new PromptItem(
                    name: $"{todayStr}.{todayPromptList.Count + 1}",
                    content: request.Content,
                    modelId: request.ModelId,
                    topP: request.TopP,
                    temperature: request.Temperature,
                    maxToken: request.MaxToken,
                    frequencyPenalty: request.FrequencyPenalty,
                    presencePenalty: request.PresencePenalty,
                    stopSequences: request.StopSequences,
                    numsOfResults: request.NumsOfResults,
                    tactic: "1",
                    aiming: 1,
                    parentTac: ""
                );
            }
            else
            {
                // 如果有id，就先找到对应的promptItem
                var oldPrompt = await base.GetObjectAsync(p => p.Id == request.Id);
                string name = oldPrompt.Name;
                string oldTactic = oldPrompt.Tactic;
                int oldAiming = oldPrompt.Aiming;

                if (request.IsNewTactic || request.IsNewSubTactic)
                {
                    var parentTac = request.IsNewTactic ? oldPrompt.ParentTac : oldPrompt.Tactic;
                    List<PromptItem> fullList = await base.GetFullListAsync(p => p.FullVersion.StartsWith($"{name}-T{parentTac}"));
                    toSavePromptItem = new PromptItem(
                        name: name,
                        content: request.Content,
                        modelId: request.ModelId,
                        topP: request.TopP,
                        temperature: request.Temperature,
                        maxToken: request.MaxToken,
                        frequencyPenalty: request.FrequencyPenalty,
                        presencePenalty: request.PresencePenalty,
                        stopSequences: request.StopSequences,
                        numsOfResults: request.NumsOfResults,
                        tactic: $"{fullList.Count + 1}",
                        aiming: 1,
                        parentTac: parentTac
                    );
                }
                else
                {
                    if (request.IsNewAiming) // 不改变分支
                    {
                        List<PromptItem> fullList = await base.GetFullListAsync(p =>
                            p.FullVersion.StartsWith(oldPrompt.FullVersion.Substring(0, oldPrompt.FullVersion.LastIndexOf('A'))
                            ));
                        toSavePromptItem = new PromptItem(
                            name: name,
                            content: request.Content,
                            modelId: request.ModelId,
                            topP: request.TopP,
                            temperature: request.Temperature,
                            maxToken: request.MaxToken,
                            frequencyPenalty: request.FrequencyPenalty,
                            presencePenalty: request.PresencePenalty,
                            stopSequences: request.StopSequences,
                            numsOfResults: request.NumsOfResults,
                            tactic: oldTactic,
                            aiming: fullList.Count + 1,
                            parentTac: oldPrompt.ParentTac
                        );
                    }
                    // todo 是否允许重新生成？
                    // return toSavePromptItem;
                }
            }

            #endregion

            await base.SaveObjectAsync(toSavePromptItem);

            return toSavePromptItem;
        }


        /// <summary>
        /// 输入一个 id，构建所对应的 PromptItem 的版本树，包含自己，父版本，递归直到root
        /// 即从该节点到root节点的最短路径
        /// </summary>
        /// <param name="promptItemId">提示词 Item 的 Id</param>
        /// <returns >版本树</returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<TreeNode<PromptItem>> GenerateVersionHistoryTree(int promptItemId)
        {
            // 找到对应的promptItem
            var promptItem = await this.GetObjectAsync(p => p.Id == promptItemId);
            if (promptItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            return await this.GenerateVersionHistoryTree(promptItem.FullVersion).ConfigureAwait(false);
        }

        /// <summary>
        /// 输入一个版本号，构建子版本树，包含自己，父版本，递归直到root
        /// 即从该节点到root节点的最短路径
        /// </summary>
        /// <param name="curVersion">当前版本号</param>
        /// <returns>版本树</returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<TreeNode<PromptItem>> GenerateVersionHistoryTree(string curVersion)
        {
            #region 找到对应的promptItem

            var promptItem = await this.GetObjectAsync(p => p.FullVersion == curVersion);
            if (promptItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            #endregion

            List<PromptItem> fullList = await this.GetFullListAsync(p => p.Name == promptItem.Name);
            // 根据 FullVersion, 将list转为Dictionary
            var itemMapByVersion = fullList.ToDictionary(p => p.FullVersion, p => p);

            // 根据 ParentTac, 将list转为Dictionary<string,List<PromptItem>>
            var itemGroupByParentTac = fullList.GroupBy(p => p.ParentTac)
                .ToDictionary(p => p.Key, p => p.ToList());

            // 从root版本, 生成TreeNode，然后循环构建版本树
            PromptItem rootItem = itemMapByVersion[$"{promptItem.Name}-T1-A1"];

            var rootNode = new TreeNode<PromptItem>(rootItem.FullVersion, promptItem);
            foreach (var childItem in itemGroupByParentTac[rootItem.Tactic])
            {
                var childNode = new TreeNode<PromptItem>(childItem.FullVersion, childItem);
                rootNode.Children.Add(childNode);
            }


            return rootNode;
        }

        public async Task<PromptItem_HistoryScoreResponse> GenerateVersionHistoryList(string curVersion)
        {
            List<string> versionHistoryList = new List<string>();
            List<int> scoreHistoryList = new List<int>();

            #region 找到对应的promptItem

            var curItem = await this.GetObjectAsync(p => p.FullVersion == curVersion);
            if (curItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            #endregion

            List<PromptItem> fullList = await this.GetFullListAsync(p => p.Name == curItem.Name, p => p.FullVersion, OrderingType.Ascending);
            // // 根据 FullVersion, 将list转为Dictionary
            // var itemMapByVersion = fullList.ToDictionary(p => p.FullVersion, p => p);
            // // 根据 ParentTac, 将list转为Dictionary<string,List<PromptItem>>
            // var itemGroupByParentTac = fullList.GroupBy(p => p.ParentTac)
            //     .ToDictionary(p => p.Key, p => p.ToList());
            //
            // // PromptItem rootItem = itemMapByVersion[$"{curItem.Name}-T1-A1"];
            // while (string.IsNullOrWhiteSpace(curItem.ParentTac))
            // {
            //     versionHistoryList.Add(curItem.FullVersion);
            //     
            //     curItem = itemMapByVersion[curItem.ParentTac];
            // }
            var index = fullList.IndexOf(curItem);
            if (index != -1)
            {
                for (var i = 0; i < index; i++)
                {
                    versionHistoryList.Add(fullList[i].FullVersion);
                    scoreHistoryList.Add(fullList[i].EvaluationScore);
                }
            }

            return new PromptItem_HistoryScoreResponse(versionHistoryList, scoreHistoryList);
        }


        public async Task<PromptItem_HistoryScoreResponse> getHistoryScore(int promptItemId)
        {
            List<string> versionHistoryList = new List<string>();
            List<int> scoreHistoryList = new List<int>();

            #region 找到对应的promptItem

            var curItem = await this.GetObjectAsync(p => p.Id == promptItemId);
            if (curItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            #endregion

            List<PromptItem> fullList = await this.GetFullListAsync(p => p.Name == curItem.Name, p => p.FullVersion, OrderingType.Ascending);
            // // 根据 FullVersion, 将list转为Dictionary
            // var itemMapByVersion = fullList.ToDictionary(p => p.FullVersion, p => p);
            // // 根据 ParentTac, 将list转为Dictionary<string,List<PromptItem>>
            // var itemGroupByParentTac = fullList.GroupBy(p => p.ParentTac)
            //     .ToDictionary(p => p.Key, p => p.ToList());
            //
            // // PromptItem rootItem = itemMapByVersion[$"{curItem.Name}-T1-A1"];
            // while (string.IsNullOrWhiteSpace(curItem.ParentTac))
            // {
            //     versionHistoryList.Add(curItem.FullVersion);
            //     
            //     curItem = itemMapByVersion[curItem.ParentTac];
            // }
            var index = fullList.IndexOf(curItem);
            if (index != -1)
            {
                for (var i = 0; i < index; i++)
                {
                    versionHistoryList.Add(fullList[i].FullVersion);
                    scoreHistoryList.Add(fullList[i].EvaluationScore);
                }
            }

            return new PromptItem_HistoryScoreResponse(versionHistoryList, scoreHistoryList);
        }

        public async void UpdateEvalScore(int promptItemId)
        {
            var promptItem = await this.GetObjectAsync(p => p.Id == promptItemId);
            if (promptItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            List<PromptResult> promptResults = await _promptResultService.GetFullListAsync(p => p.PromptItemId == promptItemId);


            int sum = 0;
            int cnt = 0;
            foreach (var promptResult in promptResults)
            {
                sum += promptResult.HumanScore < 0 ? (promptResult.RobotScore < 0 ? 0 : promptResult.RobotScore) : promptResult.HumanScore;
                if (promptResult.HumanScore < 0 && promptResult.RobotScore < 0)
                {
                    continue;
                }

                cnt++;
            }

            if (cnt != 0)
            {
                promptItem.UpdateEvalScore((int)sum / cnt);

                await this.SaveObjectAsync(promptItem);
            }
        }
    }
}
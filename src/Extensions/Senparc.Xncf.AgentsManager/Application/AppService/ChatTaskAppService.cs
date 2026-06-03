using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    [ApiAuthorize]
    public class ChatTaskAppService : AppServiceBase
    {
        private readonly ChatTaskService _chatTaskService;

        public ChatTaskAppService(IServiceProvider serviceProvider, ChatTaskService chatTaskService) : base(serviceProvider)
        {
            _chatTaskService = chatTaskService;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<ChatTask_GetListResponse>> GetList(int chatGroupId, int agentTemplateId, int pageIndex, int pageSize, string filter = "")
        {
            return await this.GetResponseAsync<ChatTask_GetListResponse>(async (response, logger) =>
                  {
                      var chatGroupIdList = new List<int>();
                      if (agentTemplateId > 0)
                      {
                          var agentTemplateService = base.GetRequiredService<AgentTemplateAppService>();
                          var memberService = base.GetRequiredService<ChatGroupMemberService>();
                          var chatGroupList = await memberService.GetFullListAsync(z => z.AgentTemplateId == agentTemplateId);
                           chatGroupIdList = chatGroupList.Select(z => z.ChatGroupId).ToList();

                          //chatTaskIdList = this._chatTaskService.GetFullList(z=> chatGroupIdList.Contains(z.ChatGroupId)).Select
                      }

                      var seh = new SenparcExpressionHelper<ChatTask>();
                      seh.ValueCompare
                          .AndAlso(chatGroupId > 0, z => z.ChatGroupId == chatGroupId)
                          .AndAlso(agentTemplateId > 0, z => chatGroupIdList.Contains(z.ChatGroupId));
                      //增加模糊搜索任务
                      seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(filter), _ => _.Name.Contains(filter));
                      var where = seh.BuildWhereExpression();

                      var list = await this._chatTaskService.GetObjectListAsync(pageIndex, pageSize, where, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                      return new ChatTask_GetListResponse()
                      {
                          ChatTaskList = this._chatTaskService.Mapping<ChatTaskDto>(list)
                      };
                  });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<ChatTask_GetItemResponse>> GetItem(int id)
        {
            return await this.GetResponseAsync<ChatTask_GetItemResponse>(async (response, logger) =>
            {
                var chatTask = await this._chatTaskService.GetObjectAsync(z => z.Id == id);

                return new ChatTask_GetItemResponse()
                {
                    ChatTaskDto = this._chatTaskService.Mapping<ChatTaskDto>(chatTask)
                };
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> ForceStop(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await ForceStopInternalAsync(new List<int> { id });
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> ForceStopBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await ForceStopInternalAsync(ids);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(new List<int> { id });
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> DeleteBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(ids);
                return result;
            });
        }

        private async Task<string> ForceStopInternalAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "未提供任务 ID";
            }

            var idSet = ids.Distinct().ToList();
            var taskList = await _chatTaskService.GetFullListAsync(z => idSet.Contains(z.Id));
            var cache = base.GetRequiredService<IBaseObjectCacheStrategy>();

            var changed = 0;
            var skipped = 0;

            foreach (var task in taskList)
            {
                if (task.Status == ChatTask_Status.Finished || task.Status == ChatTask_Status.Cancelled)
                {
                    skipped++;
                    continue;
                }

                await _chatTaskService.SetStatus(ChatTask_Status.Cancelled, task);
                await cache.RemoveFromCacheAsync(_chatTaskService.GetChatTaskRunCacheKey(task.Id));
                changed++;
            }

            return $"强制停止完成：成功 {changed} 条，跳过 {skipped} 条";
        }

        private async Task<string> DeleteInternalAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "未提供任务 ID";
            }

            var idSet = ids.Distinct().ToList();
            var taskList = await _chatTaskService.GetFullListAsync(z => idSet.Contains(z.Id));
            var taskIds = taskList.Select(z => z.Id).ToList();
            var historyService = base.GetRequiredService<ChatGroupHistoryService>();
            var cache = base.GetRequiredService<IBaseObjectCacheStrategy>();

            if (taskIds.Count > 0)
            {
                var histories = await historyService.GetFullListAsync(z => taskIds.Contains(z.ChatTaskId));
                foreach (var history in histories)
                {
                    await historyService.DeleteObjectAsync(history);
                }
            }

            foreach (var task in taskList)
            {
                await cache.RemoveFromCacheAsync(_chatTaskService.GetChatTaskRunCacheKey(task.Id));
                await _chatTaskService.DeleteObjectAsync(task);
            }

            return $"删除任务完成：成功 {taskList.Count} 条";
        }
    }
}

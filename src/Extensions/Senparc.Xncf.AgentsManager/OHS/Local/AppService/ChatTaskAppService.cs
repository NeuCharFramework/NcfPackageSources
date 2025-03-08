using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
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
    }
}

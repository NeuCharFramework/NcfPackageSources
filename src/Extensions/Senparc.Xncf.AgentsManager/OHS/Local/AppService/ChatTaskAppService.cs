using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    public class ChatTaskAppService : AppServiceBase
    {
        private readonly ChatTaskService _chatTaskService;
        public ChatTaskAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<ChatTask_GetListResponse>> GetList(int chatGroupId, int pageIndex, int pageSize)
        {
            return await this.GetResponseAsync<ChatTask_GetListResponse>(async (response, logger) =>
            {
                var list = await this._chatTaskService.GetObjectListAsync(pageIndex, pageSize, z => z.ChatGroupId == chatGroupId, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

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

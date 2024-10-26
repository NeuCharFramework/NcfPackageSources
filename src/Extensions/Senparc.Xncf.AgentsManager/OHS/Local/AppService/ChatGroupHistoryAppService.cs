using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    public class ChatGroupHistoryAppService : AppServiceBase
    {
        private readonly ChatGroupHistoryService _chatGroupHistoryService;

        public ChatGroupHistoryAppService(IServiceProvider serviceProvider, ChatGroupHistoryService chatGroupHistoryService) : base(serviceProvider)
        {
            this._chatGroupHistoryService = chatGroupHistoryService;
        }

        [ApiBind]
        public async Task<AppResponseBase<ChatGroupHistory_GetListResponse>> GetList(int chatTaskId, int nextHistoryId, int pageIndex, int pageSize)
        {
            return await this.GetResponseAsync<ChatGroupHistory_GetListResponse>(async (response, logger) =>
            {
                var list = await this._chatGroupHistoryService.
                        GetObjectListAsync(pageIndex, pageSize,
                        z => z.Id > nextHistoryId && z.ChatTaskId == chatTaskId,
                        z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);

                var result = new ChatGroupHistory_GetListResponse()
                {
                    ChatGroupHistories = this._chatGroupHistoryService.Mapping<ChatGroupHistoryDto>(list)
                };

                foreach (var historyDto in result.ChatGroupHistories)
                {
                    historyDto.Message = _chatGroupHistoryService.GetRawMessage(historyDto.Message);
                }

                return result;
            });
        }
    }
}

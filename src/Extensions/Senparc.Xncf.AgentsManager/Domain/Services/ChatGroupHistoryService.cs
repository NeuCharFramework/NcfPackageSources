using Senparc.CO2NET;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Exceptions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupHistoryService : ServiceBase<ChatGroupHistory>
    {
        public ChatGroupHistoryService(IRepositoryBase<ChatGroupHistory> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        //[ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task CreateHistory(ChatGroupHistoryDto chatGroupHistoryDto)
        {
            try
            {
                ChatGroupHistory chatGroupHistory = new ChatGroupHistory(chatGroupHistoryDto);
                await base.SaveObjectAsync(chatGroupHistory);
            }
            catch (Exception ex)
            {
                new AgentsManagerException(ex.Message, ex, false);
                //throw;
            }

        }

        /// <summary>
        /// 获取干净的消息信息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string GetRawMessage(string message)
        {
            var arr = message.Split(new[] { $"{Environment.NewLine}--------------------{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length >= 2)
            {
                return arr[1].Trim();
            }

            return message;
        }

    }
}

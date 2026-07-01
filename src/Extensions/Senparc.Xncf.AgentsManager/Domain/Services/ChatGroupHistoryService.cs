/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupHistoryService.cs
    文件功能描述：ChatGroupHistoryService 服务逻辑
    
    
    创建标识：Senparc - 20241017
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

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
        public async Task<ChatGroupHistory> CreateHistory(ChatGroupHistoryDto chatGroupHistoryDto)
        {
            try
            {
                ChatGroupHistory chatGroupHistory = new ChatGroupHistory(chatGroupHistoryDto);
                await base.SaveObjectAsync(chatGroupHistory);
                return chatGroupHistory;
            }
            catch (Exception ex)
            {
                new AgentsManagerException(ex.Message, ex, false);
                throw;
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

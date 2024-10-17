﻿using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatTaskService : ServiceBase<ChatTask>
    {
        public ChatTaskService(IRepositoryBase<ChatTask> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<ChatTask> CreateTask(ChatTaskDto chatTaskDto)
        {
            var chatTask = new ChatTask(chatTaskDto);

            //TODO:需要缓存，以便快速读取

            await base.SaveObjectAsync(chatTask);

            return chatTask;
            //return base.Mapping<ChatTaskDto>(chatTask);
        }

        public async Task SetStatus(ChatTask_Status status, ChatTask chatTask)
        {
            chatTask.ChangeStatus(status);
            await base.SaveObjectAsync(chatTask);
        }

    }
}

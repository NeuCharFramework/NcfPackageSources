using AutoGen.Core;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatTaskService : ServiceBase<ChatTask>
    {
        public ChatTaskService(IRepositoryBase<ChatTask> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 获取缓存中记录正在运行的 ChatTask 的 Key
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public string GetChatTaskRunCacheKey(int taskId)
        {
            return $"ChatTask-Running:{taskId}";
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

        /// <summary>
        /// 关闭未完成的任务
        /// </summary>
        /// <param name="beforeStartDateTime">只是筛选在此时间之前的未完成（Chatting 任务）</param>
        /// <returns></returns>
        public async Task CloseUnfinishedTasksAsync(DateTime beforeStartDateTime)
        {
            var unfinishTasks = await base.GetObjectListAsync(0, 0, z => z.StartTime < beforeStartDateTime && z.Status == ChatTask_Status.Chatting, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
            foreach (var unfinishedTask in unfinishTasks)
            {
                SenparcTrace.SendCustomLog($"处理未完成任务({unfinishedTask.Id})", $"任务：{unfinishedTask.Name}，开始时间：{unfinishedTask.StartTime}，状态：{unfinishedTask.Status}");
                unfinishedTask.ChangeStatus(ChatTask_Status.Cancelled);
                await base.SaveObjectAsync(unfinishedTask);
                SenparcTrace.SendCustomLog($"处理未完成任务({unfinishedTask.Id})", $"处理完成，当前状态：{unfinishedTask.Status}");
            }
        }
    }
}

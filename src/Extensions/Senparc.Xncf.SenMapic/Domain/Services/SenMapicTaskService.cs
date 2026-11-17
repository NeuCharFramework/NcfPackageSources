/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: SenMapicTaskService.cs
    File description: SenMapicTaskService related implementation
    
    
    Created by: Senparc - 20250114
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.SenMapic.Domain.SiteMap;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.SenMapic.Domain.Services
{
    public class SenMapicTaskService : ServiceBase<SenMapicTask>
    {
        private readonly SenMapicTaskItemService _senMapicTaskIitemService;

        public SenMapicTaskService(IRepositoryBase<SenMapicTask> repo, IServiceProvider serviceProvider,
            SenMapicTaskItemService senMapicTaskIitemService)
            : base(repo, serviceProvider)
        {
            this._senMapicTaskIitemService = senMapicTaskIitemService;
        }

        public async Task<SenMapicTask> CreateTaskAsync(string name, string startUrl, 
            int maxThread, int maxBuildMinutes, int maxDeep, int maxPageCount, bool startImmediately)
        {
            var task = new SenMapicTask(name, startUrl, maxThread, 
                maxBuildMinutes, maxDeep, maxPageCount);
            await SaveObjectAsync(task);
            
            if (startImmediately)
            {
                await StartTaskAsync(task);
            }
            
            return task;
        }

        private async Task SaveTaskStateAsync(SenMapicTask task, Action<SenMapicTask> updateAction)
        {
            try
            {
                updateAction(task);
                await SaveObjectAsync(task);
            }
            catch (Exception ex)
            {
                task.Error(ex.Message);
                await SaveObjectAsync(task);
                throw;
            }
        }

        public async Task StartTaskAsync(SenMapicTask task)
        {
            await SaveTaskStateAsync(task, t => t.Start());

            //Legacy comment translated to English.
            var engine = new SenMapicEngine(
                serviceProvider: _serviceProvider,
                urls: new[] { task.StartUrl },
                maxThread: task.MaxThread,
                maxBuildMinutesForSingleSite: task.MaxBuildMinutes,
                maxDeep: task.MaxDeep,
                maxPageCount: task.MaxPageCount
            );

            //Legacy comment translated to English.
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = engine.Build();
                    await SaveTaskStateAsync(task, t =>
                    {
                        t.IncrementCrawledPages();
                        t.Complete();
                    });
                }
                catch (Exception ex)
                {
                    await SaveTaskStateAsync(task, t => t.Error(ex.Message));
                }
            });
        }

    }
}
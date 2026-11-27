/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicTaskService.cs
    File description: SenMapicTaskService related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes task persistence and task-item persistence services.
        /// </summary>
        /// <param name="repo">The sitemap task repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="senMapicTaskIitemService">The service used to persist crawl results.</param>
        public SenMapicTaskService(IRepositoryBase<SenMapicTask> repo, IServiceProvider serviceProvider,
            SenMapicTaskItemService senMapicTaskIitemService)
            : base(repo, serviceProvider)
        {
            this._senMapicTaskIitemService = senMapicTaskIitemService;
        }

        /// <summary>
        /// Creates a waiting sitemap crawl task and optionally starts it immediately.
        /// </summary>
        /// <param name="name">The task display name.</param>
        /// <param name="startUrl">The root URL to crawl.</param>
        /// <param name="maxThread">The crawler concurrency limit.</param>
        /// <param name="maxBuildMinutes">The maximum crawl duration.</param>
        /// <param name="maxDeep">The maximum link depth.</param>
        /// <param name="maxPageCount">The maximum page count.</param>
        /// <param name="startImmediately">Whether to transition the task to running immediately.</param>
        /// <returns>The persisted task entity.</returns>
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

        /// <summary>
        /// Applies a task state transition and persists it, recording an error state if persistence fails.
        /// </summary>
        /// <param name="task">The task entity to update.</param>
        /// <param name="updateAction">The state transition to apply.</param>
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

        /// <summary>
        /// Marks a task as running and schedules its sitemap crawl in a background operation.
        /// Completion and failure are persisted to the task entity.
        /// </summary>
        /// <param name="task">The task to start.</param>
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

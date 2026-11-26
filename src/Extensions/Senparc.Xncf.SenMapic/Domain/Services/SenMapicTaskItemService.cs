/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicTaskItemService.cs
    File description: SenMapicTaskItemService related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.SenMapic.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.SenMapic.Domain.SiteMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Domain.Services
{
    public class SenMapicTaskItemService : ServiceBase<SenMapicTaskItem>
    {
        /// <summary>
        /// Initializes sitemap task-item persistence operations.
        /// </summary>
        /// <param name="repo">The task-item repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public SenMapicTaskItemService(IRepositoryBase<SenMapicTaskItem> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Loads crawl items for a task and optionally filters URLs by domain.
        /// </summary>
        /// <param name="taskId">The sitemap task identifier.</param>
        /// <param name="domain">Optional domain substring filter.</param>
        /// <returns>A mapped paged-list result.</returns>
        public async Task<PagedList<SenMapicTaskItem_ListItemDto>> GetTaskItems(int taskId,string domain)
        {
            var seh = new SenparcExpressionHelper<SenMapicTaskItem>();
            seh.ValueCompare
                .AndAlso(true, x => x.TaskId == taskId)
                .AndAlso(!domain.IsNullOrEmpty(),x=> x.Url.Contains(domain));
            var where = seh.BuildWhereExpression();
            var fullList = await base.GetFullListAsync(where);
            return fullList.ToDtoPagedList<SenMapicTaskItem, SenMapicTaskItem_ListItemDto>(this);
        }

        /// <summary>
        /// Converts collected URL data into task-item entities and persists them as a batch.
        /// </summary>
        /// <param name="task">The parent crawl task.</param>
        /// <param name="urlDatas">Collected URL data keyed by URL.</param>
        /// <returns>A task that completes after all items are saved.</returns>
        public async Task SaveTaskItemsAsync(SenMapicTask task, Dictionary<string, UrlData> urlDatas)
        {
            var itemList = new List<SenMapicTaskItem>();
            foreach (var urlData in urlDatas)
            {
                var taskItem = new SenMapicTaskItem(task, urlData.Value);
                itemList.Add(taskItem);
            }
            await base.SaveObjectListAsync(itemList);
        }

    }
}

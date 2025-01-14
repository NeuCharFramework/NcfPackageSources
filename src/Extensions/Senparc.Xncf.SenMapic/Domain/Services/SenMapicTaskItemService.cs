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
        public SenMapicTaskItemService(IRepositoryBase<SenMapicTaskItem> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

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

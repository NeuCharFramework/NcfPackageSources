using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
namespace Senparc.Xncf.KnowledgeBase.Services
{
    public class KnowledgeBaseItemService : ServiceBase<KnowledgeBaseItem>
    {
        public KnowledgeBaseItemService(IRepositoryBase<KnowledgeBaseItem> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        //TODO: 更多业务方法可以写到这里
        public async Task<IEnumerable<KnowledgeBaseItemDto>> GetKnowledgeBasesDetailList(int PageIndex, int PageSize)
        {
            List<KnowledgeBaseItemDto> selectListItems = null;
            List<KnowledgeBaseItem> knowledgeBasesDetail = (await GetFullListAsync(_ => true).ConfigureAwait(false)).OrderByDescending(_ => _.AddTime).ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBaseItemDto>>(knowledgeBasesDetail);
            return selectListItems;
        }

        /// <summary>
        /// 根据知识库ID获取关联的 KnowledgeBaseItem 列表（用于配置页回显）
        /// </summary>
        public async Task<IEnumerable<KnowledgeBaseItemDto>> GetListByKnowledgeBaseIdAsync(int knowledgeBaseId)
        {
            var list = await GetFullListAsync(_ => _.KnowledgeBasesId == knowledgeBaseId).ConfigureAwait(false);
            return Mapper.Map<List<KnowledgeBaseItemDto>>(list.OrderBy(_ => _.ChunkIndex).ThenBy(_ => _.AddTime));
        }

        public async Task CreateOrUpdateAsync(KnowledgeBaseItemDto dto)
        {
            KnowledgeBaseItem knowledgeBasesDetail;
            if (dto.Id == 0)
            {
                knowledgeBasesDetail = new KnowledgeBaseItem(dto);
            }
            else
            {
                knowledgeBasesDetail = await GetObjectAsync(_ => _.Id == dto.Id);
                knowledgeBasesDetail.Update(dto);
            }
            await SaveObjectAsync(knowledgeBasesDetail);
        }

    }

}

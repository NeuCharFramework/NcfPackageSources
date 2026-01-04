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
        public async Task<IEnumerable<KnowledgeBasesDetalDto>> GetKnowledgeBasesDetailList(int PageIndex, int PageSize)
        {
            List<KnowledgeBasesDetalDto> selectListItems = null;
            List<KnowledgeBaseItem> knowledgeBasesDetail = (await GetFullListAsync(_ => true).ConfigureAwait(false)).OrderByDescending(_ => _.AddTime).ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBasesDetalDto>>(knowledgeBasesDetail);
            return selectListItems;
        }

        public async Task CreateOrUpdateAsync(KnowledgeBasesDetalDto dto)
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

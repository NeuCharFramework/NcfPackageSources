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
    public class KnowledgeBasesDetailService : ServiceBase<KnowledgeBasesDetail>
    {
        public KnowledgeBasesDetailService(IRepositoryBase<KnowledgeBasesDetail> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        //TODO: 更多业务方法可以写到这里
        public async Task<IEnumerable<KnowledgeBasesDetailDto>> GetKnowledgeBasesDetailList(int PageIndex, int PageSize)
        {
            List<KnowledgeBasesDetailDto> selectListItems = null;
            List<KnowledgeBasesDetail> knowledgeBasesDetail = (await GetFullListAsync(_ => true).ConfigureAwait(false)).OrderByDescending(_ => _.AddTime).ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBasesDetailDto>>(knowledgeBasesDetail);
            return selectListItems;
        }

        public async Task CreateOrUpdateAsync(KnowledgeBasesDetailDto dto)
        {
            KnowledgeBasesDetail knowledgeBasesDetail;
            if (String.IsNullOrEmpty(dto.Id))
            {
                knowledgeBasesDetail = new KnowledgeBasesDetail(dto);
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

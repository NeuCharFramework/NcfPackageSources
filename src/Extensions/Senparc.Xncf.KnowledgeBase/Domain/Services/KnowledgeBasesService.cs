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
    public class KnowledgeBasesService : ServiceBase<KnowledgeBases>
    {
        public KnowledgeBasesService(IRepositoryBase<KnowledgeBases> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        //TODO: 更多业务方法可以写到这里
        public async Task<IEnumerable<KnowledgeBasesDto>> GetKnowledgeBasesList(int PageIndex, int PageSize)
        {
            List<KnowledgeBasesDto> selectListItems = null;
            List<KnowledgeBases> knowledgeBases = (await GetFullListAsync(_ => true).ConfigureAwait(false)).OrderByDescending(_ => _.AddTime).ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBasesDto>>(knowledgeBases);
            return selectListItems;
        }

        public async Task CreateOrUpdateAsync(KnowledgeBasesDto dto)
        {
            KnowledgeBases knowledgeBases;
            if (dto.Id == 0)
            {
                knowledgeBases = new KnowledgeBases(dto);
            }
            else
            {
                knowledgeBases = await GetObjectAsync(_ => _.Id == dto.Id);
                knowledgeBases.Update(dto);
            }
            await SaveObjectAsync(knowledgeBases);
        }

    }

}

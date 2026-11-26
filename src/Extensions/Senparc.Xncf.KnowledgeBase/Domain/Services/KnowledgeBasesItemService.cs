/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBasesItemService.cs
    File description: KnowledgeBasesItemService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

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
        /// <summary>
        /// Initializes knowledge-base chunk persistence operations.
        /// </summary>
        /// <param name="repo">The knowledge-base item repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public KnowledgeBaseItemService(IRepositoryBase<KnowledgeBaseItem> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Returns a paged-compatible list of all knowledge-base item DTOs ordered newest first.
        /// The current implementation materializes the complete source list before mapping.
        /// </summary>
        /// <param name="PageIndex">The requested page index; currently retained for API compatibility.</param>
        /// <param name="PageSize">The requested page size; currently retained for API compatibility.</param>
        /// <returns>All mapped knowledge-base item DTOs.</returns>
        public async Task<IEnumerable<KnowledgeBaseItemDto>> GetKnowledgeBasesDetailList(int PageIndex, int PageSize)
        {
            List<KnowledgeBaseItemDto> selectListItems = null;
            List<KnowledgeBaseItem> knowledgeBasesDetail = (await GetFullListAsync(_ => true).ConfigureAwait(false)).OrderByDescending(_ => _.AddTime).ToList();
            selectListItems = this.Mapper.Map<List<KnowledgeBaseItemDto>>(knowledgeBasesDetail);
            return selectListItems;
        }

        /// <summary>
        /// Loads all chunks belonging to one knowledge base in chunk and creation order.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <returns>The mapped chunk DTOs.</returns>
        public async Task<IEnumerable<KnowledgeBaseItemDto>> GetListByKnowledgeBaseIdAsync(int knowledgeBaseId)
        {
            var list = await GetFullListAsync(_ => _.KnowledgeBasesId == knowledgeBaseId).ConfigureAwait(false);
            return Mapper.Map<List<KnowledgeBaseItemDto>>(list.OrderBy(_ => _.ChunkIndex).ThenBy(_ => _.AddTime));
        }

        /// <summary>
        /// Creates a new chunk or updates an existing chunk from the supplied DTO.
        /// </summary>
        /// <param name="dto">The chunk values to persist.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an existing chunk cannot be resolved for update.</exception>
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

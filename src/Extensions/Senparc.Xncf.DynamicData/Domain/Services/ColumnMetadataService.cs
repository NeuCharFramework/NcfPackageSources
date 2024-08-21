using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class ColumnMetadataService : ServiceBase<ColumnMetadata>
    {
        public ColumnMetadataService(IRepositoryBase<ColumnMetadata> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<List<ColumnMetadataDto>> GetColumnDtos(int tableId)
        {
            var columns = await base.GetFullListAsync(z => z.TableMetadataId == tableId);
            var columnDtos = base.Mapper.Map<List<ColumnMetadataDto>>(columns);
            return columnDtos;
        }
    }
}

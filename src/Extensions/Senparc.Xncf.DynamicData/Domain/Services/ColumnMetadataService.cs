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
            //TODO: 添加 .ToDto<T>扩展方法
            var columnDtos = columns.Select(z=> base.Mapper.Map<ColumnMetadataDto>(z)).ToList();
            return columnDtos;
        }
    }
}

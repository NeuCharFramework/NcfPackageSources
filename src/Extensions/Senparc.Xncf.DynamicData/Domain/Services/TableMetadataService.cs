using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class TableMetadataService : ServiceBase<TableMetadata>
    {
        public TableMetadataService(IRepositoryBase<TableMetadata> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<TableMetadataDto> GetTableMetadataDtoAsync(int tableId)
        {
            //var tableMetadata = await base.GetObjectAsync(z => z.Id == tableId, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, z => z.Include(t => t.ColumnMetadatas));

            var tableMetadata =  base.GetObject(z => z.Id == tableId, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, z => z.Include(t => t.ColumnMetadatas));

            Console.WriteLine("tableMetadata:" + tableMetadata?.TableName);

            var result = base.Mapper.Map<TableMetadataDto>(tableMetadata);
            return result;
        }
    }
}

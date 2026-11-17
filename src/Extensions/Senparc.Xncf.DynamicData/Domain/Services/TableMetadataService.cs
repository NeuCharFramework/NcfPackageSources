/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: TableMetadataService.cs

    File description: TableMetadataService related implementation

    

    

    Created by: Senparc - 20240719

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
            var tableMetadata = await base.GetObjectAsync(z => z.Id == tableId, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, z => z.Include(t => t.ColumnMetadatas));

            var result = base.Mapper.Map<TableMetadataDto>(tableMetadata);
            return result;
        }

        public async Task<TableMetadataDto> GetTableMetadataDtoAsync(string name)
        {
            var tableMetadata = await base.GetObjectAsync(z => z.TableName==name, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, z => z.Include(t => t.ColumnMetadatas));

            var result = base.Mapper.Map<TableMetadataDto>(tableMetadata);
            return result;
        }
    }
}

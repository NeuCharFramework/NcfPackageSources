/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableMetadataService.cs
    File description: TableMetadataService related implementation

    Created by: Senparc - 20240719

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

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
        /// <summary>
        /// Initializes the table metadata service.
        /// </summary>
        /// <param name="repo">The table metadata repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public TableMetadataService(IRepositoryBase<TableMetadata> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Loads table metadata and its column metadata by numeric identifier.
        /// </summary>
        /// <param name="tableId">The table metadata identifier.</param>
        /// <returns>The mapped DTO, or a mapper result for a missing entity.</returns>
        public async Task<TableMetadataDto> GetTableMetadataDtoAsync(int tableId)
        {
            var tableMetadata = await base.GetObjectAsync(z => z.Id == tableId, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, z => z.Include(t => t.ColumnMetadatas));

            var result = base.Mapper.Map<TableMetadataDto>(tableMetadata);
            return result;
        }

        /// <summary>
        /// Loads table metadata and its column metadata by table name.
        /// </summary>
        /// <param name="name">The table name to match.</param>
        /// <returns>The mapped DTO, or a mapper result for a missing entity.</returns>
        public async Task<TableMetadataDto> GetTableMetadataDtoAsync(string name)
        {
            var tableMetadata = await base.GetObjectAsync(z => z.TableName==name, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, z => z.Include(t => t.ColumnMetadatas));

            var result = base.Mapper.Map<TableMetadataDto>(tableMetadata);
            return result;
        }
    }
}

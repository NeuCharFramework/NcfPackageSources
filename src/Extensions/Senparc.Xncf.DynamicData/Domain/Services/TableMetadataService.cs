/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TableMetadataService.cs
    文件功能描述：TableMetadataService 相关实现
    
    
    创建标识：Senparc - 20240719
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

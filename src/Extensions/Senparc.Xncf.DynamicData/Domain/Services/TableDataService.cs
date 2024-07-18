using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class TableDataService : ServiceBase<TableData>
    {
        public TableDataService(IRepositoryBase<TableData> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        //public async Task<TableDataDto> GetTableData(int id)
        //{
        //    var tableData = await base.GetObjectAsync(z=>z.Id == id,z=> z.Id , Ncf.Core.Enums.OrderingType.Ascending,z=>z.Include(typeof()))
        //}
    }
}

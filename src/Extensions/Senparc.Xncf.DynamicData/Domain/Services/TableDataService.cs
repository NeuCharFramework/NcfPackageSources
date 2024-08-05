using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET.Trace;
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

        public async Task<bool> InsertData(List<TableDataDto> tableDataDtos)
        {
            try
            {
                await base.BeginTransactionAsync(async () =>
                {
                    var datas = new List<TableData>();
                    foreach (var item in tableDataDtos)
                    {
                        var tableData = new TableData(item.TableId, item.ColumnMetadataId, item.CellValue);
                    }
                    await base.SaveObjectListAsync(datas);
                });

                return true;
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                return false;
            }
        }
    }
}

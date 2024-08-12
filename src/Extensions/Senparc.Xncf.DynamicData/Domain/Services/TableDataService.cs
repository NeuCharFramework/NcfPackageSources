using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Models;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Models.Extensions;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class TableDataService : ServiceBase<TableData>
    {
        private readonly ColumnMetadataService _columnMetadataService;

        public TableDataService(ColumnMetadataService columnMetadataService, IRepositoryBase<TableData> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            this._columnMetadataService = columnMetadataService;
        }

        /// <summary>
        /// 根据模板创建 TableDataDto 列表
        /// </summary>
        /// <param name="tableId"></param>
        /// <returns></returns>
        public async Task<DataTemplate> GetTableDataTemplateAsync(int tableId)
        {
            var columnTemplate = await _columnMetadataService.GetColumnDtos(tableId);
            var dataTemplate = GetTableDataTemplate(columnTemplate);
            return dataTemplate;
        }

        /// <summary>
        /// 根据模板创建 TableDataDto 列表
        /// </summary>
        /// <param name="columnTemplate"></param>
        /// <returns></returns>
        public DataTemplate GetTableDataTemplate(ColumnTemplate columnTemplate)
        {
            //从ColumnMetadataDto中获取TableDataDto
            var tableDataDtos = new List<TableDataDto>();
            foreach (var columnMetadataDto in columnTemplate)
            {
                var tableDataDto = new TableDataDto()
                {
                    TableId = columnMetadataDto.TableMetadataId,
                    ColumnMetadataId = columnMetadataDto.Id,
                };
                tableDataDtos.Add(tableDataDto);
            }
            return new DataTemplate(columnTemplate.TableId, columnTemplate, tableDataDtos);
        }

        //public async Task<TableDataDto> GetTableData(int id)
        //{
        //    var tableData = await base.GetObjectAsync(z=>z.Id == id,z=> z.Id , Ncf.Core.Enums.OrderingType.Ascending,z=>z.Include(typeof()))
        //}

        public async Task<(bool Success, List<TableData> SucessDataList)> InsertDataAsync(DataTemplate dataTemplate)
        {
            try
            {
                var datas = new List<TableData>();
                await base.BeginTransactionAsync(async () =>
                {
                    var tableDataDtos = dataTemplate.TableDataDtos;
                    foreach (var item in tableDataDtos)
                    {
                        var tableData = new TableData(item.TableId, item.ColumnMetadataId, item.CellValue);
                        datas.Add(tableData);
                    }
                    await base.SaveObjectListAsync(datas);
                });

                return (true, datas);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SenparcTrace.BaseExceptionLog(ex);
                return (false, null);
            }
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <param name="dataTemplate"></param>
        /// <param name="dataList"></param>
        /// <param name="dataDic"></param>
        /// <returns></returns>
        public void SetData(DataTemplate dataTemplate, Dictionary<string, string> dataDic)
        {
            var dataList = dataTemplate.TableDataDtos;

            foreach (var item in dataTemplate.ColumnTemplate)
            {
                var data = dataList.FirstOrDefault(z => z.ColumnMetadataId == item.Id);
                if (data == null)
                {
                    data = new TableDataDto()
                    {
                        TableId = item.TableMetadataId,
                        ColumnMetadataId = item.Id,
                    };
                    dataList.Add(data);
                }

                if (dataDic.ContainsKey(item.ColumnName))
                {
                    data.CellValue = dataDic[item.ColumnName];
                }
            }
        }
    }
}

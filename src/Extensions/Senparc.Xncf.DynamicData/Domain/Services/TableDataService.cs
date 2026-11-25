/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TableDataService.cs
    File description: TableDataService related implementation

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

        /// <summary>
        /// Initializes the table-data service with column metadata and persistence dependencies.
        /// </summary>
        /// <param name="columnMetadataService">The service used to load table columns.</param>
        /// <param name="repo">The table-data repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public TableDataService(ColumnMetadataService columnMetadataService, IRepositoryBase<TableData> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            this._columnMetadataService = columnMetadataService;
        }

        /// <summary>
        /// Loads column metadata and creates an empty data template for a table.
        /// </summary>
        /// <param name="tableId">The table metadata identifier.</param>
        /// <returns>A template containing one data cell descriptor per column.</returns>
        public async Task<DataTemplate> GetTableDataTemplateAsync(int tableId)
        {
            var columnTemplate = await _columnMetadataService.GetColumnDtos(tableId);
            var dataTemplate = GetTableDataTemplate(columnTemplate);
            return dataTemplate;
        }

        /// <summary>
        /// Creates an empty data template from an existing column template.
        /// </summary>
        /// <param name="columnTemplate">The columns that define the template shape.</param>
        /// <returns>A data template containing empty cells for each column.</returns>
        public DataTemplate GetTableDataTemplate(ColumnTemplate columnTemplate)
        {
            //Legacy comment translated to English.
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

        /// <summary>
        /// Converts template DTOs to entities and persists them in one transaction.
        /// </summary>
        /// <param name="dataTemplate">The template containing table and column cell values.</param>
        /// <returns>
        /// A success flag and persisted entities; when persistence fails, the flag is false and the list is null.
        /// </returns>
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
        /// Applies values from a column-name dictionary to the cells in a data template.
        /// </summary>
        /// <param name="dataTemplate">The template whose cells should be created or updated.</param>
        /// <param name="dataDic">The values keyed by column name.</param>
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

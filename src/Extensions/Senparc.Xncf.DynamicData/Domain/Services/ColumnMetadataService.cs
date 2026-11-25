/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ColumnMetadataService.cs
    File description: ColumnMetadataService related implementation

    Created by: Senparc - 20240719

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DynamicData.Domain.Models;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.DynamicData.Domain.Models.Extensions;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class ColumnMetadataService : ServiceBase<ColumnMetadata>
    {
        //private TableDataService _tableDataService;
        private Lazy<TableMetadataService> _tableMetadataService;
        /// <summary>
        /// Initializes the column metadata service and its lazy table metadata dependency.
        /// </summary>
        /// <param name="repo">The column metadata repository.</param>
        /// <param name="serviceProvider">The dependency provider used to resolve table metadata.</param>
        public ColumnMetadataService(IRepositoryBase<ColumnMetadata> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            //_tableDataService = serviceProvider.GetRequiredService<TableDataService>();
            _tableMetadataService = serviceProvider.GetRequiredService<Lazy<TableMetadataService>>();
        }

        /// <summary>
        /// Creates table and column metadata from the public instance properties of an entity type when absent.
        /// </summary>
        /// <typeparam name="T">The entity type whose metadata should be discovered.</typeparam>
        /// <returns>A data template containing the table's column metadata.</returns>
        public async Task<ColumnTemplate> TryCreateTableAndColumnMetaFromEntityAsync<T>()
        {
            //Legacy comment translated to English.
            var tableName = typeof(T).FullName;
            var tableMetaDto = await _tableMetadataService.Value.GetTableMetadataDtoAsync(tableName);
            if (tableMetaDto == null)
            {
                //Legacy comment translated to English.
                //tableMetaDto = new TableMetadataDto()
                //{
                //    TableName = tableName,
                //    Description = tableName
                //};

                var tableMeta = new TableMetadata(tableName, tableName);

                await _tableMetadataService.Value.SaveObjectAsync(tableMeta);

                tableMetaDto = _tableMetadataService.Value.Mapping<TableMetadataDto>(tableMeta);

                //Legacy comment translated to English.
                var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                List<ColumnMetadata> columns = new List<ColumnMetadata>();
                foreach (var prop in props)
                {
                    //Legacy comment translated to English.
                    var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                    var defaultAttr = prop.GetCustomAttribute<DefaultValueAttribute>();

                    var columnMeta = new ColumnMetadata(tableMeta.Id, prop.Name, prop.PropertyType.Name, !isRequired, defaultAttr?.Value.ToString());
                    columns.Add(columnMeta);
                }
                await base.SaveObjectListAsync(columns);
            }

            var columnTemplate = await this.GetColumnDtos(tableMetaDto.Id);
            return columnTemplate;
        }

        /// <summary>
        /// Loads column metadata for a table and wraps the mapped DTOs in a column template.
        /// </summary>
        /// <param name="tableId">The table metadata identifier.</param>
        /// <returns>The column template for the table.</returns>
        public async Task<ColumnTemplate> GetColumnDtos(int tableId)
        {
            var columns = await base.GetFullListAsync(z => z.TableMetadataId == tableId);
            //Legacy comment translated to English.
            var columnDtos = columns.Select(z => base.Mapper.Map<ColumnMetadataDto>(z)).ToList();
            return new ColumnTemplate(tableId, columnDtos);
        }
    }
}

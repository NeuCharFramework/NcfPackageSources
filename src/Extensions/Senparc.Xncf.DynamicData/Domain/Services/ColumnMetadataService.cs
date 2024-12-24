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
        public ColumnMetadataService(IRepositoryBase<ColumnMetadata> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            //_tableDataService = serviceProvider.GetRequiredService<TableDataService>();
            _tableMetadataService = serviceProvider.GetRequiredService<Lazy<TableMetadataService>>();
        }

        /// <summary>
        /// 尝试根据实体创建表和列元数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<ColumnTemplate> TryCreateTableAndColumnMetaFromEntityAsync<T>()
        {
            //查看是否已经存在表
            var tableName = typeof(T).FullName;
            var tableMetaDto = await _tableMetadataService.Value.GetTableMetadataDtoAsync(tableName);
            if (tableMetaDto == null)
            {
                //创建对象
                //tableMetaDto = new TableMetadataDto()
                //{
                //    TableName = tableName,
                //    Description = tableName
                //};

                var tableMeta = new TableMetadata(tableName, tableName);

                await _tableMetadataService.Value.SaveObjectAsync(tableMeta);

                tableMetaDto = _tableMetadataService.Value.Mapping<TableMetadataDto>(tableMeta);

                //创建列
                var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                List<ColumnMetadata> columns = new List<ColumnMetadata>();
                foreach (var prop in props)
                {
                    //TODO:是否可为null，需要[Required]特性进行判断
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

        public async Task<ColumnTemplate> GetColumnDtos(int tableId)
        {
            var columns = await base.GetFullListAsync(z => z.TableMetadataId == tableId);
            //TODO: 添加 .ToDto<T>扩展方法
            var columnDtos = columns.Select(z => base.Mapper.Map<ColumnMetadataDto>(z)).ToList();
            return new ColumnTemplate(tableId, columnDtos);
        }
    }
}

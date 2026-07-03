/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DynamicData_Mapping.cs
    文件功能描述：DynamicData_Mapping 相关实现
    
    
    创建标识：Senparc - 20240718
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class DynamicData_TableDataConfigurationMapping : ConfigurationMappingWithIdBase<TableData, int>
    {
        public override void Configure(EntityTypeBuilder<TableData> builder)
        {
            Console.WriteLine("run DynamicData_TableDataConfigurationMapping");

            // 配置索引  
            builder.HasIndex(td => td.TableId)
                   .HasDatabaseName("idx_table_id");

            builder.HasIndex(td => td.ColumnMetadataId)
                   .HasDatabaseName("idx_column_id");

            builder.HasIndex(td => new { td.TableId, td.ColumnMetadataId })
                   .HasDatabaseName("idx_table_column");


            //builder.HasOne(td => td.TableMetadata)
            //       .WithMany(tm => tm.TableDatas)
            //       .HasForeignKey(td => td.TableMetadataId)
            //       .OnDelete(DeleteBehavior.NoAction);  // Specify NO ACTION


            builder.HasOne(td => td.ColumnMetadata)
                   .WithMany(tm=>tm.TableDatas)
                   .HasForeignKey(td => td.ColumnMetadataId)
                   .OnDelete(DeleteBehavior.NoAction);  // Specify NO ACTION

        }
    }

    [XncfAutoConfigurationMapping]
    public class DynamicData_ColumnMetadataConfigurationMapping : ConfigurationMappingWithIdBase<ColumnMetadata, int>
    {
        public override void Configure(EntityTypeBuilder<ColumnMetadata> builder)
        {
            Console.WriteLine("run DynamicData_ColumnMetadataConfigurationMapping");
            builder.HasOne(cm => cm.TableMetadata)
                   .WithMany(tm => tm.ColumnMetadatas)
                   .HasForeignKey(cm => cm.TableMetadataId)
                   .OnDelete(DeleteBehavior.NoAction);  // Specify NO ACTION  
        }
    }
}

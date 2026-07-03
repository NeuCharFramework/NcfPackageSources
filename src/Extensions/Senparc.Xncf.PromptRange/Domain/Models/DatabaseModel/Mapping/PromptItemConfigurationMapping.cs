/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItemConfigurationMapping.cs
    文件功能描述：PromptItemConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20240514
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Xml;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Senparc.Ncf.Database.Sqlite;
using System.Reflection.Emit;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class PromptPromptItemConfigurationMapping : ConfigurationMappingWithIdBase<PromptItem, int>
    {
        public override void Configure(EntityTypeBuilder<PromptItem> builder)
        {
            if (Senparc.Ncf.Core.Models.DatabaseConfigurationFactory.Instance.Current is SqliteDatabaseConfiguration)
            {
                builder.Property(e => e.EvalAvgScore).HasConversion<double>();
                builder.Property(e => e.EvalMaxScore).HasConversion<double>();
            }

            base.Configure(builder);
        }
    }
}
/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapic_TaskConfigurationMapping.cs
    文件功能描述：SenMapic_TaskConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20250114
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.SenMapic.Models
{
    [XncfAutoConfigurationMapping]
    public class SenMapic_TaskConfigurationMapping : ConfigurationMappingWithIdBase<SenMapicTask, int>
    {
        public override void Configure(EntityTypeBuilder<SenMapicTask> builder)
        {
            builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
            builder.Property(e => e.StartUrl).IsRequired().HasMaxLength(1000);
            builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        }
    }
} 
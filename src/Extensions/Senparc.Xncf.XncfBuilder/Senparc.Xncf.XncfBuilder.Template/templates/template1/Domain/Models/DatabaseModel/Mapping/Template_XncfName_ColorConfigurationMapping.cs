/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Template_XncfName_ColorConfigurationMapping.cs
    文件功能描述：Template_XncfName_ColorConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    [XncfAutoConfigurationMapping]
    public class Template_XncfName_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}

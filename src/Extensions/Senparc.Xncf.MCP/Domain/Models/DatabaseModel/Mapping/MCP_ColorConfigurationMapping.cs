/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MCP_ColorConfigurationMapping.cs
    文件功能描述：MCP_ColorConfigurationMapping 相关实现
    
    
    创建标识：Senparc - 20250325
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;

namespace Senparc.Xncf.MCP.Models
{
    [XncfAutoConfigurationMapping]
    public class MCP_ColorConfigurationMapping : ConfigurationMappingWithIdBase<Color, int>
    {
        public override void Configure(EntityTypeBuilder<Color> builder)
        {
            builder.Property(e => e.Red).IsRequired();
            builder.Property(e => e.Green).IsRequired();
            builder.Property(e => e.Blue).IsRequired();
        }
    }
}

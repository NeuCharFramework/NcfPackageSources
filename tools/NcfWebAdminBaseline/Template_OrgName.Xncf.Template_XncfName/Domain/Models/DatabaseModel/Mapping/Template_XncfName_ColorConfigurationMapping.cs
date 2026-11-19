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

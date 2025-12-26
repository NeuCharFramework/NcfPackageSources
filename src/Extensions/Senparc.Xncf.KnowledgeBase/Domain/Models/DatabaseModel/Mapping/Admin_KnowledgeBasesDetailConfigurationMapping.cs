using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    [XncfAutoConfigurationMapping]
    public class Admin_KnowledgeBasesDetailConfigurationMapping : ConfigurationMappingWithIdBase<KnowledgeBasesDetail, int>
    {
        public override void Configure(EntityTypeBuilder<KnowledgeBasesDetail> builder)
        {
            
        }
    }
}
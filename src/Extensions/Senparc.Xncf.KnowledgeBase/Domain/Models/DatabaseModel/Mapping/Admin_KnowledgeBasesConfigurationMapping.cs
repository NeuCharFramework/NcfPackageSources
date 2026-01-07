using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;

namespace Senparc.Xncf.KnowledgeBase.Models
{
    [XncfAutoConfigurationMapping]
    public class Admin_KnowledgeBasesConfigurationMapping : ConfigurationMappingWithIdBase<DatabaseModel.KnowledgeBase, int>
    {
        public override void Configure(EntityTypeBuilder<DatabaseModel.KnowledgeBase> builder)
        {
            
        }
    }
}
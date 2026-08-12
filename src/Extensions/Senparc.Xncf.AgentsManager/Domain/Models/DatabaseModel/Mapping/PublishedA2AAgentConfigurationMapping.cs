using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class PublishedA2AAgentConfigurationMapping : ConfigurationMappingWithIdBase<PublishedA2AAgent, int>
    {
        public override void Configure(EntityTypeBuilder<PublishedA2AAgent> builder)
        {
            base.Configure(builder);
            builder.HasIndex(z => z.AgentTemplateId).IsUnique();
            builder.HasIndex(z => z.PublicAgentKey).IsUnique();
            builder.HasIndex(z => z.Enable);
        }
    }
}

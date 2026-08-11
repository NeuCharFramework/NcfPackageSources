using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class RemoteAgentConfigurationMapping : ConfigurationMappingWithIdBase<RemoteAgent, int>
    {
        public override void Configure(EntityTypeBuilder<RemoteAgent> builder)
        {
            base.Configure(builder);
            builder.HasIndex(z => z.AgentCardUrl);
            builder.HasIndex(z => z.Enable);
        }
    }
}

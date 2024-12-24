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
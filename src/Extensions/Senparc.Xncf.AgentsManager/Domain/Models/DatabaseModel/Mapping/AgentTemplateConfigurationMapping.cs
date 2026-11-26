/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentTemplateConfigurationMapping.cs
    File description: AgentTemplateConfigurationMapping related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.14.0-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping
{
    [XncfAutoConfigurationMapping]
    public class AgentTemplateConfigurationMapping : ConfigurationMappingWithIdBase<AgentTemplate, int>
    {
        /// <inheritdoc/>
        public override void Configure(EntityTypeBuilder<AgentTemplate> builder)
        {

            base.Configure(builder);
            builder.HasIndex(z => z.KnowledgeBaseId);
            builder.HasIndex(z => z.AiModelId);
        }
    }
}

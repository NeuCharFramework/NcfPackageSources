/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FeedbackConfigurationMapping.cs
    File description: FeedbackConfigurationMapping related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.SystemManager.Domain.DatabaseModel;

namespace Senparc.Xncf.SystemManager.Domain
{
    [XncfAutoConfigurationMapping]
    public class FeedbackConfigurationMapping : ConfigurationMappingWithIdBase<FeedBack, int>
    {
        public override void Configure(EntityTypeBuilder<FeedBack> builder)
        {
            //builder.HasQueryFilter(z => !z.Flag);
        }
    }
}
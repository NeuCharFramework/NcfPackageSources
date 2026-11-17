/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChatConfigurationMapping.cs
    File description: PromptResultChatConfigurationMapping related implementation

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Mapping
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [XncfAutoConfigurationMapping]
    public class PromptResultChatConfigurationMapping : ConfigurationMappingWithIdBase<PromptResultChat, int>
    {
        public override void Configure(EntityTypeBuilder<PromptResultChat> builder)
        {
            base.Configure(builder);

            //Legacy comment translated to English.
            builder.HasOne<PromptResult>()
                .WithMany()
                .HasForeignKey(c => c.PromptResultId)
                .OnDelete(DeleteBehavior.Cascade);

            //Legacy comment translated to English.
            builder.HasIndex(c => c.PromptResultId);
            builder.HasIndex(c => new { c.PromptResultId, c.Sequence });

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            builder.Property(c => c.Content)
                .IsRequired();

            builder.Property(c => c.Sequence)
                .IsRequired();

            builder.Property(c => c.RoleType)
                .IsRequired()
                .HasConversion<int>(); //Legacy comment translated to English.

            //Legacy comment translated to English.
        }
    }
}

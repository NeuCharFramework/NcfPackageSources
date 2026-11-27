/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChatConfigurationMapping.cs
    File description: PromptResultChatConfigurationMapping related implementation

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Mapping
{
    /// <summary>
    /// Maps prompt-result chat messages and their cascade relationship to PromptResult.
    /// </summary>
    [XncfAutoConfigurationMapping]
    public class PromptResultChatConfigurationMapping : ConfigurationMappingWithIdBase<PromptResultChat, int>
    {
        /// <inheritdoc />
        public override void Configure(EntityTypeBuilder<PromptResultChat> builder)
        {
            base.Configure(builder);

            // Delete chat messages when their owning prompt result is deleted.
            builder.HasOne<PromptResult>()
                .WithMany()
                .HasForeignKey(c => c.PromptResultId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index the owning result for history queries and preserve message order.
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

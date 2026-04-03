using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Mapping
{
    /// <summary>
    ///PromptResultChat entity configuration mapping
    /// </summary>
    [XncfAutoConfigurationMapping]
    public class PromptResultChatConfigurationMapping : ConfigurationMappingWithIdBase<PromptResultChat, int>
    {
        public override void Configure(EntityTypeBuilder<PromptResultChat> builder)
        {
            base.Configure(builder);

            // Configure foreign key relationships
            builder.HasOne<PromptResult>()
                .WithMany()
                .HasForeignKey(c => c.PromptResultId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure index
            builder.HasIndex(c => c.PromptResultId);
            builder.HasIndex(c => new { c.PromptResultId, c.Sequence });

            // Configure field lengths and constraints
            // The Content field does not set MaxLength and uses the TEXT type of the database to support long text.
            builder.Property(c => c.Content)
                .IsRequired();

            builder.Property(c => c.Sequence)
                .IsRequired();

            builder.Property(c => c.RoleType)
                .IsRequired()
                .HasConversion<int>(); // Convert enum to int storage

            // UserFeedback and UserScore can be null and no additional configuration is required
        }
    }
}

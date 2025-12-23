using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Mapping
{
    /// <summary>
    /// PromptResultChat 实体配置映射
    /// </summary>
    [XncfAutoConfigurationMapping]
    public class PromptResultChatConfigurationMapping : ConfigurationMappingWithIdBase<PromptResultChat, int>
    {
        public override void Configure(EntityTypeBuilder<PromptResultChat> builder)
        {
            base.Configure(builder);

            // 配置外键关系
            builder.HasOne<PromptResult>()
                .WithMany()
                .HasForeignKey(c => c.PromptResultId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配置索引
            builder.HasIndex(c => c.PromptResultId);
            builder.HasIndex(c => new { c.PromptResultId, c.Sequence });

            // 配置字段长度和约束
            // Content 字段不设置 MaxLength，使用数据库的 TEXT 类型以支持长文本
            builder.Property(c => c.Content)
                .IsRequired();

            builder.Property(c => c.Sequence)
                .IsRequired();

            builder.Property(c => c.RoleType)
                .IsRequired()
                .HasConversion<int>(); // 将枚举转换为 int 存储

            // UserFeedback 和 UserScore 可以为 null，不需要额外配置
        }
    }
}





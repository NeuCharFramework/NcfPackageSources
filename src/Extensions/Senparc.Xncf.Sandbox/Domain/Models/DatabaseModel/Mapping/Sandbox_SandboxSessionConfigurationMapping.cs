using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Sandbox.Models;

[XncfAutoConfigurationMapping]
public class Sandbox_SandboxSessionConfigurationMapping : ConfigurationMappingWithIdBase<SandboxSession, int>
{
    public override void Configure(EntityTypeBuilder<SandboxSession> builder)
    {
        // SQL Server 索引键不能是 nvarchar(max)，必须带明确长度
        builder.Property(e => e.SessionId).IsRequired().HasMaxLength(64);
        builder.Property(e => e.TemplateKey).IsRequired().HasMaxLength(64);
        builder.Property(e => e.RuntimeHandle).HasMaxLength(128);
        builder.Property(e => e.AccessUrl).HasMaxLength(500);
        builder.Property(e => e.AccessToken).HasMaxLength(128);
        builder.Property(e => e.StatusMessage).HasMaxLength(1000);
        builder.Property(e => e.AdminRemark).HasMaxLength(300);
        builder.Property(e => e.Remark).HasMaxLength(300);
        builder.HasIndex(e => e.SessionId).IsUnique();
        builder.HasIndex(e => new { e.OwnerUserId, e.Status });
        builder.HasIndex(e => e.ExpiresAtUtc);
    }
}
